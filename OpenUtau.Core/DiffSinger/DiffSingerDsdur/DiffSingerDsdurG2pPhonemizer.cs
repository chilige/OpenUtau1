using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

using OpenUtau.Api;
using System.Linq;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger {
    public abstract class DiffSingerDsdurG2pPhonemizer : DiffSingerDsdurBasePhonemizer {
        protected virtual IG2p LoadBaseG2p() => new ArpabetG2p();
        //vowels and consonants of BaseG2p
        protected virtual string[] GetBaseG2pVowels() => new string[] { };
        protected virtual string[] GetBaseG2pConsonants() => new string[] { };

        protected override IG2p LoadG2p(string rootPath) {
            var dictionaryName = GetDictionaryName();
            var g2ps = new List<IG2p>();

            // Load dictionary from dsdur folder.
            var replacements_org = new Dictionary<string, string>();
            string file = Path.Combine(rootPath, dictionaryName);
            replacements_org = LoadRemapper(file);
            Dictionary<string, string> replacements = replacements_org.ToDictionary(pair => pair.Value, pair => pair.Key);

            // Load base g2p.
            var baseG2p = LoadBaseG2p();
            if (baseG2p == null) {
                return new G2pFallbacks(g2ps.ToArray());
            }
            var phonemeSymbols = new Dictionary<string, bool>();
            foreach (var v in GetBaseG2pVowels()) {
                phonemeSymbols[v] = true;
            }
            foreach (var c in GetBaseG2pConsonants()) {
                phonemeSymbols[c] = false;
            }
            foreach (var from in replacements.Keys) {
                var to = replacements[from];
                if (baseG2p.IsValidSymbol(to)) {
                    if (baseG2p.IsVowel(to)) {
                        phonemeSymbols[from] = true;
                    } else {
                        phonemeSymbols[from] = false;
                    }
                }
            }
            g2ps.Add(new G2pRemapper(baseG2p, phonemeSymbols, replacements));
            return new G2pFallbacks(g2ps.ToArray());
        }

        public Dictionary<string, string> LoadRemapper(string path) {
            var phoneDict = new Dictionary<string, string>();
            if (File.Exists(path)) {
                foreach (string line in File.ReadLines(path, System.Text.Encoding.UTF8)) {
                    string[] elements = line.Split(",");
                    try {
                        phoneDict.Add(elements[0].Trim(), elements[1].Trim());
                    } catch (Exception) {
                        Log.Information(elements[0].Trim() + "的音素无法注入 请确认是否重复或内容异常");
                    }
                }
            } else {
                Log.Information("remapper加载失败");
            }
            return phoneDict;
        }
    }
}