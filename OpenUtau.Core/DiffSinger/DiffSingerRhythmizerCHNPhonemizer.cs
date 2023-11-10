using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.IO;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using System.Text;
using System.Linq;

namespace OpenUtau.Core.DiffSinger {

    [Phonemizer("DiffSinger Rhythmizer Mandarin Phonemizer", "DIFFS CHN-RHY", "BaiTang",language: "ZH")]
    public class DiffSingerRhythmizerCHNPhonemizer : DiffSingerRhythmizerBasePhonemizer {
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseChinesePhonemizer.Romanize(lyrics);
        }
    }
    
}
