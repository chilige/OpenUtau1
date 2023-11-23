using System.Collections.Generic;

using OpenUtau.Api;

namespace OpenUtau.Core.DiffSinger {
    [Phonemizer("DiffSinger Cantonese Dsdur Phonemizer", "DIFFS CNY-DSDUR", "BaiTang", language: "DIFFS-DSDUR")]
    public class DiffSingerDsdurCNYPhonemizer : DiffSingerDsdurBasePhonemizer {
        protected override string GetDictionaryName() => "dsdict-cny.yaml";
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return BaseCantonesePhonemizer.Romanize(lyrics);
        }
    }
}