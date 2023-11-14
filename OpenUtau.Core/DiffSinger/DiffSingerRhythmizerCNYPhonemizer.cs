using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.IO;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using System.Text;
using System.Linq;
using Serilog;
using Newtonsoft.Json;

namespace OpenUtau.Core.DiffSinger {

    [Phonemizer("DiffSinger Cantonese RHY Phonemizer", "DIFFS CNY-RHY", "BaiTang", language: "ZH")]
    public class DiffSingerRhythmizerCNYPhonemizer : DiffSingerRhythmizerBasePhonemizer {

        public override void SetUpPhoneDictAndRhy() {
            LoadSingerRhythmizer("CNY");
            GetRealPhnDict("ds_CNY.txt");
            GetRhyMap();
        }

        protected override void ProcessPart(Note[][] phrase) {
            ProcessByRhyMap(phrase);
        }

        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseCantonesePhonemizer.Romanize(lyrics);
        }
    }

}
