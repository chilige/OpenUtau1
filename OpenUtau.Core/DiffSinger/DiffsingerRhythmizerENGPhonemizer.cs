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

    [Phonemizer("DiffSinger English RHY Phonemizer", "DIFFS ENG-RHY", "BaiTang", language: "EN")]
    public class DiffSingerRhythmizerENGPhonemizer : DiffSingerRhythmizerBasePhonemizer {

        public override void SetUpPhoneDictAndRhy() {
            LoadSingerRhythmizer("ENG");
            GetRealPhnDict("ds_ENG.txt");
            GetRhyMap();
        }

        protected override void ProcessPart(Note[][] phrase) {
            ProcessByRhyMap(phrase);
        }
    }

}
