namespace penta
{
    /// <summary>
    /// 전투 도중 레벨업 시 생성되는 아이템을 획득했을 때
    ///  랜덤으로 아이템을 사용 (주요 로직)
    /// </summary>
    public class LevelUpRewardItem : MonoBehaviour
    {
        ...
        private IEnumerator CO_FloatingAnimation()
        {
            float time = 0f;
            while (!isCollected)
            {
                time += Time.deltaTime * floatingSpeed;
                float yOffset = Mathf.Sin(time) * floatingHeight;
                transform.position = startPosition + new Vector3(0, yOffset, 0);
                yield return null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                CollectItem();
            }
        }

        private void CollectItem()
        {
            if (isCollected) return;
            isCollected = true;

            UseRandomGlobalItem();

            if (collectVfx != null && VFXManager.Shared != null)
            {
                var vfx = Instantiate(collectVfx, transform.position, Quaternion.identity);
                if (vfx != null)
                {
                    var ps = vfx.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                        Destroy(vfx, duration + 0.5f);
                    }
                    else
                    {
                        Destroy(vfx, 2f);
                    }
                }
            }

            Destroy(gameObject);
        }

        private void UseRandomGlobalItem()
        {
            if (GlobalItem.Shared == null)
            {
                "[LevelUpRewardItem] GlobalItem.Shared is null!".DError();
                return;
            }

            List<int> availableItems = new List<int> { 0, 1, 2, 3 }; // 0: heal, 1: god, 2: haste, 3: fever
            int randomIndex = Random.Range(0, availableItems.Count);
            int selectedItem = availableItems[randomIndex];
            bool isLevelUpReward = true;
            switch (selectedItem)
            {
                case 0: // Heal
                    if (!GlobalItem.Shared.IsHealOnCooldown)
                    {
                        GlobalItem.Shared.StartCoroutine(GlobalItem.Shared.Co_PlayerHeal(isLevelUpReward));
                    }
                    else
                    {
                        UseRandomGlobalItem();
                    }
                    break;

                case 1: // God
                    if (!GlobalItem.Shared.IsGodOnCooldown)
                    {
                        GlobalItem.Shared.StartCoroutine(GlobalItem.Shared.Co_PlayerGod(isLevelUpReward));
                    }
                    else
                    {
                        UseRandomGlobalItem();
                    }
                    break;

                case 2: // Haste
                    if (!GlobalItem.Shared.IsHasteOnCooldown)
                    {
                        GlobalItem.Shared.StartCoroutine(GlobalItem.Shared.Co_PlayerHaste(isLevelUpReward));
                    }
                    else
                    {
                        UseRandomGlobalItem();
                    }
                    break;

                case 3: // Fever
                    if (!GlobalItem.Shared.IsFeverOnCooldown)
                    {
                        GlobalItem.Shared.StartCoroutine(GlobalItem.Shared.Co_PlayerFever(isLevelUpReward));
                    }
                    else
                    {
                        UseRandomGlobalItem();
                    }
                    break;
            }
        }
    }
}
