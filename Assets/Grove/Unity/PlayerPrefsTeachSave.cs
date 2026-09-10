using Grove.Domain.Orders;
using UnityEngine;

namespace Grove.Unity
{
    internal sealed class PlayerPrefsTeachSave : ITeachSave
    {
        public const string Key = "grove.teach.order1.done";

        public bool IsOrder1TeachDone
        {
            get => PlayerPrefs.GetInt(Key, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(Key, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
