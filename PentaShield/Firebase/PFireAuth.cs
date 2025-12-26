using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using System;
using System.Text;
using System.Threading;
using UnityEngine;
using AppleAuth;
using AppleAuth.Interfaces;
using AppleAuth.Enums;
using AppleAuth.Native;

namespace penta
{
    /// <summary>
    /// Firebase Authentication 래퍼 (주요 로직)
    /// - Google/Apple 로그인
    /// - 익명 로그인
    /// - 계정 연동
    /// </summary>
    public class PFireAuth : IDisposable
    {
        private const int AUTH_STATE_TIMEOUT_MS = 15000;

        private FirebaseAuth _auth = null;
        private GoogleSignInConfiguration _googleConfig = null;
        private UniTaskCompletionSource<bool> _initializationComplete = null;
        private bool _isFirstStateChange = true;
        private FirebaseUser _previousUser = null;
        private IAppleAuthManager _appleAuthManager;

        public bool IsInitialized { get; private set; } = false;
        public bool IsAppleSupported { get; private set; } = false;
        public FirebaseUser CurrentUser => _auth?.CurrentUser;
        public bool IsLoggedIn => CurrentUser != null;

        public PFireAuth(FirebaseAuth instance)
        {
            if (instance == null)
            {
                return;
            }

            _auth = instance;

            var config = FirebaseConfig.Load();
            string webClientId = !string.IsNullOrEmpty(config.GoogleWebClientId)
                ? config.GoogleWebClientId
                : "986502625873-kvfte3eje4q3i3deo8tptdd4rmnuk0vd.apps.googleusercontent.com";

            _googleConfig = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true,
                RequestEmail = true
            };
            _initializationComplete = new UniTaskCompletionSource<bool>();

            IsInitialized = false;
            InitializeAsync().Forget();
        }

        private async UniTask<bool> InitializeAsync()
        {
            if (IsInitialized) return true;
            if (_auth == null) return false;

            try
            {
                _auth.StateChanged += OnAuthStateChanged;

                using (var cts = new CancellationTokenSource(AUTH_STATE_TIMEOUT_MS))
                {
                    try
                    {
                        await _initializationComplete.Task.AttachExternalCancellation(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _initializationComplete.TrySetResult(true);
                    }
                }

#if UNITY_IOS && !UNITY_EDITOR
                if (AppleAuthManager.IsCurrentPlatformSupported)
                {
                    var deserializer = new PayloadDeserializer();
                    _appleAuthManager = new AppleAuthManager(deserializer);
                    IsAppleSupported = true;

                    PlayerLoopHelper.AddAction(
                        PlayerLoopTiming.Update,
                        new AppleAuthPlayerLoopItem(() => _appleAuthManager)
                    );
                }
#endif

                IsInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _initializationComplete.TrySetResult(false);
                return false;
            }
        }

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            try
            {
                var currentUser = _auth.CurrentUser;

                if (_isFirstStateChange)
                {
                    _isFirstStateChange = false;
                    _initializationComplete.TrySetResult(true);
                }

                _previousUser = currentUser;
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary> 익명 로그인 (필요시) </summary>
        public async UniTask<FirebaseUser> SignInAnonymouslyIfNeededAsync()
        {
            if (!IsInitialized) return null;
            if (CurrentUser != null) return CurrentUser;

            try
            {
                AuthResult authResult = await _auth.SignInAnonymouslyAsync();
                return authResult.User;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary> Google 로그인 </summary>
        public async UniTask<FirebaseUser> SignInWithGoogleAsync()
        {
            await UniTask.WaitUntil(() => IsInitialized == true);

            try
            {
                GoogleSignIn.Configuration = _googleConfig;
                GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();
                if (googleUser == null)
                {
                    return null;
                }
                Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

                if (_auth.CurrentUser != null && _auth.CurrentUser.IsAnonymous)
                {
                    try
                    {
                        AuthResult linkResult = await _auth.CurrentUser.LinkWithCredentialAsync(credential);
                        return linkResult?.User;
                    }
                    catch (FirebaseException ex)
                    {
                        if ((AuthError)ex.ErrorCode == AuthError.CredentialAlreadyInUse)
                        {
                            FirebaseUser signedInUser = await _auth.SignInWithCredentialAsync(credential);
                            return signedInUser;
                        }
                        return null;
                    }
                }
                else
                {
                    FirebaseUser signedInUser = await _auth.SignInWithCredentialAsync(credential);
                    return signedInUser;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary> Apple 로그인 </summary>
        public async UniTask<FirebaseUser> SignInWithAppleAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            await UniTask.WaitUntil(() => IsInitialized == true);

            if (!AppleAuthManager.IsCurrentPlatformSupported)
            {
                return null;
            }

            var tcs = new UniTaskCompletionSource<IAppleIDCredential>();
            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

            _appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    if (credential is IAppleIDCredential appleIdCred)
                    {
                        tcs.TrySetResult(appleIdCred);
                    }
                    else
                    {
                        tcs.TrySetException(new Exception("Apple credential invalid"));
                    }
                },
                error =>
                {
                    tcs.TrySetException(new Exception(error.ToString()));
                }
            );

            IAppleIDCredential appleCredential = null;
            try
            {
                appleCredential = await tcs.Task;
            }
            catch (Exception e)
            {
                return null;
            }

            var idToken = Encoding.UTF8.GetString(appleCredential.IdentityToken);
            var authorizationCode = Encoding.UTF8.GetString(appleCredential.AuthorizationCode);
            var rawNonce = appleCredential.User;

            try
            {
                var credentialFirebase = OAuthProvider.GetCredential(
                    "apple.com",
                    idToken,
                    rawNonce,
                    authorizationCode
                );

                if (_auth.CurrentUser != null && _auth.CurrentUser.IsAnonymous)
                {
                    try
                    {
                        AuthResult linkResult = await _auth.CurrentUser.LinkWithCredentialAsync(credentialFirebase);
                        return linkResult?.User;
                    }
                    catch (FirebaseException ex)
                    {
                        if ((AuthError)ex.ErrorCode == AuthError.CredentialAlreadyInUse)
                        {
                            FirebaseUser signedInUser = await _auth.SignInWithCredentialAsync(credentialFirebase);
                            return signedInUser;
                        }
                        return null;
                    }
                }

                FirebaseUser signedIn = await _auth.SignInWithCredentialAsync(credentialFirebase);
                return signedIn;
            }
            catch (Exception ex)
            {
                return null;
            }
#else
            return null;
#endif
        }

        /// <summary> 로그아웃 </summary>
        public void SignOut()
        {
            if (!IsLoggedIn) return;

            _auth.SignOut();

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            GoogleSignIn.DefaultInstance?.SignOut();
#endif
        }

        /// <summary> 계정 삭제 </summary>
        public async UniTask<bool> AccountDelete()
        {
            if (!IsLoggedIn) return false;
            if (CurrentUser.IsAnonymous) return false;

            try
            {
                await CurrentUser.DeleteAsync();

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                GoogleSignIn.DefaultInstance?.SignOut();
#endif

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_auth != null)
            {
                _auth.StateChanged -= OnAuthStateChanged;
            }

            _initializationComplete?.TrySetCanceled();
            _initializationComplete = null;

            _appleAuthManager = null;
            IsAppleSupported = false;

            IsInitialized = false;
        }

        private sealed class AppleAuthPlayerLoopItem : IPlayerLoopItem
        {
            private readonly Func<IAppleAuthManager> _managerProvider;

            public AppleAuthPlayerLoopItem(Func<IAppleAuthManager> managerProvider)
            {
                _managerProvider = managerProvider ?? throw new ArgumentNullException(nameof(managerProvider));
            }

            public bool MoveNext()
            {
                var manager = _managerProvider?.Invoke();
                if (manager == null)
                {
                    return false;
                }

                manager.Update();
                return true;
            }
        }
    }
}
