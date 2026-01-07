using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    public static class PentaConst
    {

        public readonly static string kmainMenu_Scene = "main_menu@scene";
        public readonly static string KsnowAge_Scene = "snow_age@scene";
        public readonly static string KfireWorld_Scene = "fire_world@scene";
        public readonly static string KstoneAge_Scene = "stone_age@scene";

        public readonly static string Kstar = "star@sprite";
        public readonly static string SolidStar_Sprite = "solidstar@sprite";
        public readonly static string EmptyStar_Sprite = "emptystar@sprite";

        public readonly static string StageSnowAgeName = "SnowAge";
        public readonly static string StageFireWorldName = "FireWorld";
        public readonly static string StageStoneAgeName = "StoneAge";


        // User Data
        public readonly static string PrefixUserId = "user_"; // 익명 사용자 ID 접두사

        public readonly static string SaveDataFileName = "EncrypedUserData.bin";                // 로컬에 저장되는 파일 이름
        public readonly static string SaveTodoUploadDataFileName = "TodoEncrypedUserData.bin";  // Firebase에 업로드 해야하는 파일 이름
        public readonly static string SaveRankFileName = "EncrypedRankData.bin";          // 랭킹 데이터 저장 파일 이름
        public readonly static string SaveBackupFileName = "BackupUserData.bin";        // 백업 저장 파일

        public static string SaveDataFileDefaultPath => Application.persistentDataPath;
        public static string SaveDataFilePath => Path.Combine(SaveDataFileDefaultPath, SaveDataFileName);
        public static string SaveTodoUploadFilePath => Path.Combine(SaveDataFileDefaultPath, SaveTodoUploadDataFileName);
        public static string SaveRankFilePath => Path.Combine(SaveDataFileDefaultPath, SaveRankFileName);
        public static string SaveBackupFilePath => Path.Combine(SaveDataFileDefaultPath, SaveBackupFileName);

        public readonly static string kUpgradeImgPlayerDamage = "upgrade_playerdamage@sprite";
        public readonly static string kUpgradeImgPlayerProjCount = "upgrade_projcount@sprite";
        public readonly static string kUpgradeImgPlayerRate = "upgrade_ratecount@sprite";
        public readonly static string kUpgradeImgPlayerHeal = "upgrade_playerheal@sprite";

        public readonly static string kUpgradeImgElementalWater = "upgrade_water@sprite";
        public readonly static string kUpgradeImgElementalFlame = "upgrade_flame@sprite";
        public readonly static string kUpgradeImgElementalThunder = "upgrade_thunder@sprite";
        public readonly static string kUpgradeImgElementalCurse = "upgrade_curse@sprite";
        public readonly static string kUpgradeImgElementalStone = "upgrade_stone@sprite";

        public readonly static string kUpgradeImgGuardHeal = "upgrade_guardheal@sprite";

        // Animation
        public readonly static int tAttack = Animator.StringToHash("Attack");
        public readonly static int tWalk = Animator.StringToHash("Walk");
        public readonly static int tHit = Animator.StringToHash("Hit");
        public readonly static int tLeftAttack = Animator.StringToHash("LeftAttackMotion");
        public readonly static int tRightAttack = Animator.StringToHash("RightAttackMotion");
        public readonly static int tMove = Animator.StringToHash("Move");
        public readonly static int kScale = Animator.StringToHash("Scale");
        #region GlobalItem & Icon
        public readonly static string kGIconIce = "gitem_ice@sprite";
        public readonly static string kGIconFlame = "gitem_flame@sprite";
        public readonly static string kGIconThunder = "gitem_thunder@sprite";
        public readonly static string kGIconCurse = "gitem_curse@sprite";
        public readonly static string kGIconStone = "gitem_stone@sprite";

        public readonly static string kGIceMeteo = "icemeteo@gitem";
        public readonly static string kGstoneRadial = "stoneradial@gitem";
        public readonly static string kGfireMeteo = "firemeteo@gitem";
        public readonly static string kGthunderCrash = "thundercrash@gitem";
        public readonly static string kGmultiCurse = "multicurse@gitem";

        public readonly static string KGplayerItemFlag = "playeritemflag@canvas";
        public readonly static string KGIconGod = "gitem_god@sprite";
        public readonly static string KGIconFever = "gitem_fever@sprite";
        public readonly static string KGIconHeal = "gitem_heal@sprite";
        public readonly static string KGIconHaste = "gitem_haste@sprite";
        public readonly static string KGIconRandomBox = "gitem_randombox@sprite";
        public readonly static string KGIconRandomCard = "gitem_randomcard@sprite";
        public readonly static string KGIconGoldenBox = "gitem_goldenbox@sprite";
        public readonly static string KGIconCacheBox = "gitem_randomcachebox@sprite";
        public readonly static string kIconEli = "eli@sprite";
        public readonly static string kIconStone = "stone@sprite";

        public readonly static string kGFxHeal = "gitem_heal@effect";
        public readonly static string kGFxHaste = "gitem_haste@effect";
        public readonly static string kGFxFever = "gitem_fever@effect";
        public readonly static string kGFxGod = "gitem_god@effect";

        public readonly static string kGLevelUpReward = "levelup_rewardbox@item";

        #endregion

        #region VFX
        public readonly static string KVfxLandingEnd = "landingend@effect";
        public readonly static string KVfxEnemyDie = "enemydie@effect";
        public readonly static string KVfxPlayerLevelUp = "playerlevelup@effect";
        public readonly static string KVfxUpgradeDisable = "upgrade_disable@fx";
        #endregion

        #region Layer Names
        public const string LAYER_PLAYER = "Player";
        public const string LAYER_GUARD = "Guard";
        public const string LAYER_ENEMY = "Enemy";
        public const string LAYER_GROUND = "Ground";
        public const string LAYER_PROJ = "Proj";
        #endregion

        public readonly static string KSIconKorea = "korea@region";
        public readonly static string KSIconJap = "japan@region";
        public readonly static string KSIconChi = "china@region";
        public readonly static string KSIconEng = "america@region";


        public readonly static string KGFxTrapFireHit = "trap_fire@hit";
        public readonly static string KGFxTrapStoneHit = "trap_stone@hit";
    }
}