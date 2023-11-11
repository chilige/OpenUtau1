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

    [Phonemizer("DiffSinger Japanese RHY Phonemizer", "DIFFS JPN-RHY", "BaiTang", language: "JA")]
    public class DiffSingerRhythmizerJPNPhonemizer : DiffSingerRhythmizerBasePhonemizer {
        public override void SetUpPhoneDictAndRhy() {
            LoadSingerRhythmizer("JPN");
            GetRealPhnDict("ds_JPN.txt");
            GetRhyMap();
        }

        protected override void ProcessPart(Note[][] phrase) {
            ProcessByRhyMap(phrase);
        }

        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseJapanesePhonemizer.Romanize(lyrics);
        }
    }
}
