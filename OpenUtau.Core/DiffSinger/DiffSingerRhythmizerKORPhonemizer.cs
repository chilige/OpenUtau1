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

    [Phonemizer("DiffSinger Korean RHY Phonemizer", "DIFFS KOR-RHY", "BaiTang", language: "KO")]
    public class DiffSingerRhythmizerKORPhonemizer : DiffSingerRhythmizerBasePhonemizer {
        public override void SetUpPhoneDictAndRhy() {
            LoadSingerRhythmizer("KOR");
            GetRealPhnDict("ds_KOR.txt");
            GetRhyMap();
        }
        
        protected override void ProcessPart(Note[][] phrase) {
            ProcessByRhyMap(phrase);
        }

        // Roman TODO
    }

}
