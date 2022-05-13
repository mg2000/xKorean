using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xKorean
{
    internal sealed class CommonSingleton
    {
        private CommonSingleton() { }

        public static readonly Lazy<CommonSingleton> mInstance = new Lazy<CommonSingleton>(() => new CommonSingleton());

        public static CommonSingleton Instance {
            get {
                return mInstance.Value;
            }
        }

        public string DBPath
        {
            get;
            set;
        }

        public byte[] OneTitleHeader
        {
            get;
            set;
        }

        public byte[] SeriesXSTitleHeader
        {
            get;
            set;
        }

        public byte[] PlayAnywhereTitleHeader
        {
            get;
            set;
        }

        public byte[] PlayAnywhereSeriesTitleHeader
        {
            get;
            set;
        }

        public byte[] WindowsTitleHeader
        {
            get;
            set;
        }
    }
}
