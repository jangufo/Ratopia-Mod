using System;

namespace RatopiaMod
{
    /// <summary>
    /// 游戏存档自定义设置
    /// </summary>
    [Serializable]
    public class GameSaveCustomSettings
    {
        /// <summary>
        /// 共享仓库
        /// </summary>
        public bool shareStorage = false;

        /// <summary>
        /// 自定义市民皮肤
        /// </summary>
        public bool customCitizenSkin = false;
    }
}
