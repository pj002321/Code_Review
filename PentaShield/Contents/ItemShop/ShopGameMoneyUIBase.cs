using penta;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace penta
{
    public class ShopGameMoneyUIBase : MonoBehaviour
    {
        public Func<int> MoneyGetter { get; private set; } = null;
        public Action<int> MoneySetter { get; private set; } = null;

        public bool IsInitalized { get; private set; } = false;

        protected TextMeshProUGUI textMeshPro = null;

        public void Initalize(Func<int> moneyGetter, Action<int> moneySetter, TextMeshProUGUI tmp)
        {
            if (moneyGetter == null && moneySetter == null) { return; }

            MoneyGetter = moneyGetter;
            MoneySetter = moneySetter;
            textMeshPro = tmp;
            IsInitalized = true;
        }       // Initalize()

        private void OnEnable()
        {
            if (UserDataManager.Shared != null)
            {
                UserDataManager.Shared.OnDataUpdated -= HandleUserDataUpdated;
                UserDataManager.Shared.OnDataUpdated += HandleUserDataUpdated;
            }

            if (IsInitalized)
            {
                UpdateText().Forget();
            }
        }

        private void OnDisable()
        {
            if (UserDataManager.Shared != null)
            {
                UserDataManager.Shared.OnDataUpdated -= HandleUserDataUpdated;
            }
        }

        private void HandleUserDataUpdated(UserData _)
        {
            if (!IsInitalized) return;
            UpdateText().Forget();
        }

        public async UniTask UpdateText()
        {
            await UniTask.Yield();
            if (MoneyGetter == null)
            {
                $"{this.name} : Getter Function Is NULL!".EError();
                return;
            }
            if (textMeshPro == null)
            {
                $"{this.name} : TextMeshPro Is NULL!".EError();
                return;
            }
            
            textMeshPro.text = MoneyGetter.Invoke().ToString();
        }
    }
}