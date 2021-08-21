using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BemaniDiscord
{
    enum GameState { Playing, Menu };
    abstract class GameBase
    {
        public IntPtr handle;
        public abstract void LoadOffsets();
        public abstract bool IsLoaded();
        public abstract void Init();
        public abstract GameState GetState();
        public abstract string GetSongString();
        public abstract string ImgName();
    }
}
