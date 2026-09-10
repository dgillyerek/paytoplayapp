using Grove.Domain.Orders;
using UnityEngine;

namespace Grove.Unity
{
    internal sealed class PlayerPrefsTeachSave : ITeachSave
    {
        public const string Key = ThinTeach.PersistKey;

        public bool IsOrder1TeachDone
        {
            get
            {
                if (PlayerPrefs.GetInt(ThinTeach.PersistKey, 0) == 1)
                {
                    return true;
                }

                if (PlayerPrefs.GetInt(ThinTeach.LegacyPersistKey, 0) != 1)
                {
                    return false;
                }

                PlayerPrefs.SetInt(ThinTeach.PersistKey, 1);
                PlayerPrefs.Save();
                return true;
            }
            set
            {
                PlayerPrefs.SetInt(ThinTeach.PersistKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
