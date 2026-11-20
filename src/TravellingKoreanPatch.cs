using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Travelling.Korean
{
    public static class KoreanPatch
    {
        private static bool _initialized = false;
        private static Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.Ordinal);
        private static Dictionary<string, string> _translationsIgnoreCase = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _normalizedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _speakerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _tagMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _locKeyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static TMP_FontAsset _fallbackFontAsset;

        public static TMP_FontAsset FallbackFont { get { return _fallbackFontAsset; } }

        private static readonly Regex _wsRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex _tripleBracketRegex = new Regex(@"\[\[\[\s*([^\]]+?)\s*\]\s*\]\]", RegexOptions.Compiled);
        private static readonly Regex _tripleOpenBracketRegex = new Regex(@"\[\[\[\s*([^\[\]]+?)\s*\]\s*\]", RegexOptions.Compiled);
        private static readonly Regex _bracketWithParticleRegex = new Regex(@"\[\[\s*(?<key>[^\[\]]+?)\s*\]\s*\](?<space>\s*)(?<particle>\([이을은으과]\)[가를는로와]|[이가을를은는과와]\([가이을를은는와과]\)|[가-힣]+)?", RegexOptions.Compiled);
        private static readonly Regex _linkColorInnerRegex = new Regex(@"<color=(?<color>#[0-9a-fA-F]{6,8})><link=""(?<id>[^""]+)"">(?<b1><b>)?(?<label>.*?)(?<b2></b>)?</link></color>(?<space>\s*)(?<particle>\([이을은으과]\)[가를는로와]|[이가을를은는과와]\([가이을를은는와과]\)|[가-힣]+)?", RegexOptions.Compiled);
        private static readonly Regex _linkColorOuterRegex = new Regex(@"<link=""(?<id>[^""]+)""><color=(?<color>#[0-9a-fA-F]{6,8})>(?<b1><b>)?(?<label>.*?)(?<b2></b>)?</color></link>(?<space>\s*)(?<particle>\([이을은으과]\)[가를는로와]|[이가을를은는과와]\([가이을를은는와과]\)|[가-힣]+)?", RegexOptions.Compiled);
        private static readonly Regex _linkPlainRegex = new Regex(@"<link=""(?<id>[^""]+)"">(?<b1><b>)?(?<label>[^<]*?)(?<b2></b>)?</link>(?<space>\s*)(?<particle>\([이을은으과]\)[가를는로와]|[이가을를은는과와]\([가이을를은는와과]\)|[가-힣]+)?", RegexOptions.Compiled);
        private static readonly Regex _parenParticleRegex = new Regex(@"(?<word>[가-힣0-9]+)(?<tags>(?:</link>|</color>|</b>|</i>|</u>)*)(?:\((?<p1>이|을|은|으|과|와)\)(?<p2>가|를|는|로|와|과)|(?<alt1>이|가|을|를|은|는|과|와|으로|로)\((?<alt2>가|이|를|을|는|은|와|과|로|으로)\))", RegexOptions.Compiled);
        private static readonly Regex _quoteSourceRegex = new Regex(@"^\s*[—–-]\s*(?<author>[^,]+),\s*(?<work>.+)$", RegexOptions.Compiled);
        private static readonly Regex _multiBracketPrefixRegex = new Regex(@"^(?<num>\d+\.\s*)?(?<brackets>(?:\[[^\]]+\]\s*)+)(?<body>.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _singleBracketRegex = new Regex(@"\[(?<inner>[^\]]+)\]", RegexOptions.Compiled);
        private static readonly Regex _inlineStarRegex = new Regex(@"^(?<pre><i>\s*<sprite=""inline""\s+name=""star"">\s*)(?<inner>.*?)(?<post></i>)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _journalRedBarRegex = new Regex(@"^<uppercase>(?<title>.*?)</uppercase>\s*--\s*(?<desc>.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _skillModRegex = new Regex(@"^(?<i1><i>)?\s*(?<skill>[A-Za-z][A-Za-z\s]*?)\s+(?<mod>[+-]\d+)\s*(?<i2></i>)?$", RegexOptions.Compiled);
        private static readonly Regex _skillVsRegex = new Regex(@"(?<skill>[A-Za-z][A-Za-z\s]*?)\s*(?<mod>[+-]\d+)\s*(?:vs|대)\s*(?<diff>\d+)", RegexOptions.Compiled);
        private static readonly Regex _companionModRegex = new Regex(@"(?<skill>[A-Za-z][A-Za-z\s]*?):\s*(?<mod>[+-]\d+)", RegexOptions.Compiled);
        private static readonly Regex _smolSkillPickerRegex = new Regex(@"^(?<pre><sprite=""SmolSkillImages""[^>]*>\s*<space=[^>]*>\s*<sprite=""SmolSkillImages""[^>]*>\s*)(?<skill>[A-Za-z][A-Za-z\s]*?)(?<post>\s*<color=.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _aspectPrefRegex = new Regex(@"(?<prefix>(?:[+-]\d+\s*)?(?:좋아함|싫어함):\s*)(?<aspect>[A-Za-z][A-Za-z\s]*)", RegexOptions.Compiled);
        private static readonly Regex _qtyLeadingRegex = new Regex(@"^(?<num>\d+)\s+(?<item>[A-Za-zÀ-ÿ].*)$", RegexOptions.Compiled);
        private static readonly Regex _qtyTrailingRegex = new Regex(@"^(?<item>[A-Za-zÀ-ÿ].*?)\s+(?<num>\d+\+|\d+-\d+|\d+-|[><=]+\s*\d+|\d+)$", RegexOptions.Compiled);
        private static readonly Regex _noItemRegex = new Regex(@"^No\s+(?<item>[A-Za-zÀ-ÿ].*)$", RegexOptions.Compiled);
        private static readonly Regex _sizeWrapRegex = new Regex(@"^(?<open><(?:size|cspace)=[^>]+>)(?<inner>[^<]+)(?<close></(?:size|cspace)>)$", RegexOptions.Compiled);

        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void UnityLog(string msg) { UnityEngine.Debug.Log(msg); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void UnityLogError(string msg) { UnityEngine.Debug.LogError(msg); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string GetUnityDataPath() { return Application.dataPath; }

        private static void SafeLog(string msg)
        {
            try { UnityLog(msg); } catch {}
        }

        private static void SafeLogError(string msg)
        {
            try { UnityLogError(msg); } catch {}
        }

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                SafeLog("[TravellingKoreanPatch] Initializing Korean Patch v9 (Unified Master Engine)...");

                // 1. Initialize built-in maps first
                InitSpeakerMap();
                InitTagMap();
                InitLocKeyMap();

                string koreanDir = FindKoreanDir();
                SafeLog("[TravellingKoreanPatch] Resolved Korean directory: " + (koreanDir ?? "NULL"));

                // 2. Locate translations.txt
                string transFile = null;
                if (!string.IsNullOrEmpty(koreanDir))
                {
                    string candidate = Path.Combine(koreanDir, "translations.txt");
                    if (File.Exists(candidate)) transFile = candidate;
                }

                if (string.IsNullOrEmpty(transFile))
                {
                    try
                    {
                        string data = GetUnityDataPath();
                        if (!string.IsNullOrEmpty(data))
                        {
                            string c1 = Path.Combine(data, "Korean", "translations.txt");
                            if (File.Exists(c1)) transFile = c1;
                            else if (File.Exists(Path.Combine(data, "translations.txt"))) transFile = Path.Combine(data, "translations.txt");
                            else if (File.Exists(Path.Combine(data, "Managed", "translations.txt"))) transFile = Path.Combine(data, "Managed", "translations.txt");
                        }
                    }
                    catch {}
                }

                if (string.IsNullOrEmpty(transFile))
                {
                    string hardcoded = @"C:\Program Files (x86)\Steam\steamapps\common\Travelling at Night Demo\travelling_Data\Korean\translations.txt";
                    if (File.Exists(hardcoded)) transFile = hardcoded;
                }

                if (!string.IsNullOrEmpty(transFile) && File.Exists(transFile))
                {
                    int count = 0;
                    using (var reader = new StreamReader(transFile, Encoding.UTF8))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                            string k = null;
                            string v = null;

                            int tabIdx = line.IndexOf('\t');
                            if (tabIdx >= 0)
                            {
                                k = line.Substring(0, tabIdx);
                                v = line.Substring(tabIdx + 1);
                            }
                            else
                            {
                                int searchFrom = 0;
                                if (line.StartsWith("[q="))
                                {
                                    int closeBracket = line.IndexOf(']');
                                    if (closeBracket > 0) searchFrom = closeBracket + 1;
                                }
                                int eqIdx = line.IndexOf('=', searchFrom);
                                if (eqIdx > 0)
                                {
                                    k = line.Substring(0, eqIdx);
                                    v = line.Substring(eqIdx + 1);
                                }
                            }

                            if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v))
                            {
                                string unescapedK = Unescape(k);
                                string unescapedV = Unescape(v);
                                _translations[unescapedK] = unescapedV;
                                _translationsIgnoreCase[unescapedK] = unescapedV;

                                string trimmedK = unescapedK.Trim();
                                if (!string.IsNullOrEmpty(trimmedK))
                                {
                                    if (!_translations.ContainsKey(trimmedK))
                                        _translations[trimmedK] = unescapedV;
                                    if (!_translationsIgnoreCase.ContainsKey(trimmedK))
                                        _translationsIgnoreCase[trimmedK] = unescapedV;
                                }

                                string normKey = CollapseWhitespace(unescapedK);
                                if (!string.IsNullOrEmpty(normKey) && !_normalizedMap.ContainsKey(normKey))
                                {
                                    _normalizedMap[normKey] = unescapedV;
                                }
                                count++;
                            }
                        }
                    }
                    SafeLog("[TravellingKoreanPatch] Successfully loaded " + count + " translation entries from: " + transFile);
                }
                else
                {
                    SafeLogError("[TravellingKoreanPatch] CRITICAL: translations.txt NOT FOUND!");
                }

                // 3. Setup Dynamic Fallback Font Asset with Full Weight & Italic Support
                try { InitFallbackFont(koreanDir); } catch {}

                _initialized = true;
                SafeLog("[TravellingKoreanPatch] Initialization complete!");
            }
            catch (Exception ex)
            {
                SafeLogError("[TravellingKoreanPatch] Initialization error: " + ex);
            }
        }

        private static string FindKoreanDir()
        {
            try
            {
                string data = GetUnityDataPath();
                if (!string.IsNullOrEmpty(data))
                {
                    string p = Path.Combine(data, "Korean");
                    if (Directory.Exists(p) && File.Exists(Path.Combine(p, "translations.txt"))) return p;
                    if (File.Exists(Path.Combine(data, "translations.txt"))) return data;
                    string managedP = Path.Combine(data, "Managed");
                    if (File.Exists(Path.Combine(managedP, "translations.txt"))) return managedP;
                }
            }
            catch {}

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDir))
                {
                    string clean = baseDir.TrimEnd('\\', '/');
                    if (clean.EndsWith("Managed", StringComparison.OrdinalIgnoreCase))
                    {
                        var parent = Directory.GetParent(clean);
                        if (parent != null)
                        {
                            string p = Path.Combine(parent.FullName, "Korean");
                            if (Directory.Exists(p) && File.Exists(Path.Combine(p, "translations.txt"))) return p;
                        }
                    }
                    string p1 = Path.Combine(baseDir, "travelling_Data", "Korean");
                    if (Directory.Exists(p1) && File.Exists(Path.Combine(p1, "translations.txt"))) return p1;
                    string p2 = Path.Combine(baseDir, "Korean");
                    if (Directory.Exists(p2) && File.Exists(Path.Combine(p2, "translations.txt"))) return p2;
                    if (File.Exists(Path.Combine(baseDir, "translations.txt"))) return baseDir;
                }
            }
            catch {}

            string steamKorean = @"C:\Program Files (x86)\Steam\steamapps\common\Travelling at Night Demo\travelling_Data\Korean";
            if (Directory.Exists(steamKorean) && File.Exists(Path.Combine(steamKorean, "translations.txt")))
            {
                return steamKorean;
            }

            return null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void InitFallbackFont(string koreanDir)
        {
            try
            {
                List<string> fontFiles = new List<string>();
                if (!string.IsNullOrEmpty(koreanDir))
                {
                    fontFiles.Add(Path.Combine(koreanDir, "font.ttf"));
                    fontFiles.Add(Path.Combine(koreanDir, "NanumGothic.ttf"));
                    fontFiles.Add(Path.Combine(koreanDir, "NotoSansKR-Regular.otf"));
                }
                try
                {
                    string data = Application.dataPath;
                    if (!string.IsNullOrEmpty(data))
                    {
                        fontFiles.Add(Path.Combine(data, "Korean", "font.ttf"));
                        fontFiles.Add(Path.Combine(data, "font.ttf"));
                        fontFiles.Add(Path.Combine(data, "Managed", "font.ttf"));
                    }
                }
                catch {}
                fontFiles.Add(@"C:\Windows\Fonts\malgun.ttf");
                fontFiles.Add(@"C:\Windows\Fonts\gulim.ttc");

                string fontPath = null;
                foreach (var f in fontFiles)
                {
                    if (!string.IsNullOrEmpty(f) && File.Exists(f)) { fontPath = f; break; }
                }

                if (fontPath != null)
                {
                    SafeLog("[TravellingKoreanPatch] Creating dynamic font asset from: " + fontPath);
                    _fallbackFontAsset = TMP_FontAsset.CreateFontAsset(fontPath, 0, 90, 9, (GlyphRenderMode)4165, 1024, 1024);
                    if (_fallbackFontAsset == null)
                    {
                        Font unityFont = new Font(fontPath);
                        _fallbackFontAsset = TMP_FontAsset.CreateFontAsset(unityFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                    }
                    if (_fallbackFontAsset != null)
                    {
                        _fallbackFontAsset.name = "Korean_Dynamic_Fallback";

                        var weightTable = _fallbackFontAsset.fontWeightTable;
                        if (weightTable == null || weightTable.Length < 10)
                        {
                            weightTable = new TMP_FontWeightPair[10];
                            var field = typeof(TMP_FontAsset).GetField("m_FontWeightTable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (field != null) field.SetValue(_fallbackFontAsset, weightTable);
                        }
                        for (int i = 0; i < weightTable.Length; i++)
                        {
                            weightTable[i].regularTypeface = _fallbackFontAsset;
                            weightTable[i].italicTypeface = _fallbackFontAsset;
                        }

                        if (TMP_Settings.fallbackFontAssets == null)
                            TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();
                        if (!TMP_Settings.fallbackFontAssets.Contains(_fallbackFontAsset))
                            TMP_Settings.fallbackFontAssets.Add(_fallbackFontAsset);

                        if (TMP_Settings.defaultFontAsset != null)
                            EnsureFallbackFont(TMP_Settings.defaultFontAsset);

                        SafeLog("[TravellingKoreanPatch] Dynamic fallback font asset initialized with full weight/italic support!");
                    }
                }
            }
            catch (Exception fontEx)
            {
                SafeLogError("[TravellingKoreanPatch] Error creating fallback font: " + fontEx);
            }
        }

        public static void EnsureFallbackFont(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null || _fallbackFontAsset == null) return;
            if (fontAsset == _fallbackFontAsset) return;

            try
            {
                AddFallbackInternal(fontAsset);

                if (fontAsset.fontWeightTable != null)
                {
                    for (int i = 0; i < fontAsset.fontWeightTable.Length; i++)
                    {
                        AddFallbackInternal(fontAsset.fontWeightTable[i].regularTypeface);
                        AddFallbackInternal(fontAsset.fontWeightTable[i].italicTypeface);
                    }
                }
            }
            catch {}
        }

        private static void AddFallbackInternal(TMP_FontAsset target)
        {
            if (target == null || target == _fallbackFontAsset) return;
            try
            {
                if (target.fallbackFontAssetTable == null)
                    target.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!target.fallbackFontAssetTable.Contains(_fallbackFontAsset))
                    target.fallbackFontAssetTable.Add(_fallbackFontAsset);
            }
            catch {}
        }

        public static void EnsureTMPText(TMP_Text textComp)
        {
            if (textComp == null) return;
            if (!_initialized) Initialize();

            if (textComp.font != null)
            {
                EnsureFallbackFont(textComp.font);
            }
        }

        public static void ProcessTMPText(TMP_Text textComp)
        {
            if (textComp == null) return;
            if (!_initialized) Initialize();

            try
            {
                EnsureFallbackFont(textComp.font);

                string cur = textComp.text;
                if (!string.IsNullOrEmpty(cur))
                {
                    string trans = Translate(cur);
                    if (!object.ReferenceEquals(trans, cur) && trans != cur)
                    {
                        textComp.text = trans;
                    }
                }
            }
            catch {}
        }

        // ==========================================
        // UNIVERSAL KOREAN PARTICLE AUTO-CORRECTION
        // ==========================================
        public static string FixKoreanParticle(string precedingWord, string particle)
        {
            if (string.IsNullOrEmpty(precedingWord) || string.IsNullOrEmpty(particle))
                return particle;

            char lastHangul = '\0';
            for (int i = precedingWord.Length - 1; i >= 0; i--)
            {
                char c = precedingWord[i];
                if (c >= 0xAC00 && c <= 0xD7A3)
                {
                    lastHangul = c;
                    break;
                }
            }

            bool hasCoda = false;
            int jongseong = 0;
            if (lastHangul != '\0')
            {
                int code = lastHangul - 0xAC00;
                jongseong = code % 28;
                hasCoda = (jongseong != 0);
            }
            else
            {
                // Check trailing digit
                char lastDigit = '\0';
                for (int i = precedingWord.Length - 1; i >= 0; i--)
                {
                    char c = precedingWord[i];
                    if (c >= '0' && c <= '9') { lastDigit = c; break; }
                }
                if (lastDigit == '\0') return particle;
                // Korean number reading: 0(영),1(일),3(삼),6(육),7(칠),8(팔) have coda; 1,7,8 have rieul(8) coda
                if (lastDigit == '0' || lastDigit == '3' || lastDigit == '6') { hasCoda = true; jongseong = 1; }
                else if (lastDigit == '1' || lastDigit == '7' || lastDigit == '8') { hasCoda = true; jongseong = 8; }
                else { hasCoda = false; jongseong = 0; }
            }

            if (particle == "이" || particle == "가" || particle == "(이)가" || particle == "이(가)" || particle == "가(이)")
                return hasCoda ? "이" : "가";
            if (particle == "은" || particle == "는" || particle == "(은)는" || particle == "은(는)" || particle == "는(은)")
                return hasCoda ? "은" : "는";
            if (particle == "을" || particle == "를" || particle == "(을)를" || particle == "을(를)" || particle == "를(을)")
                return hasCoda ? "을" : "를";
            if (particle == "과" || particle == "와" || particle == "(과)와" || particle == "(와)과" || particle == "과(와)" || particle == "와(과)")
                return hasCoda ? "과" : "와";
            if (particle == "아" || particle == "야")
                return hasCoda ? "아" : "야";
            if (particle == "(으)로" || particle == "으로(로)" || particle == "로(으로)")
                return (hasCoda && jongseong != 8) ? "으로" : "로";

            if (particle.StartsWith("이라") || particle.StartsWith("라"))
            {
                string rest = particle.StartsWith("이라") ? particle.Substring(2) : particle.Substring(1);
                return hasCoda ? ("이라" + rest) : ("라" + rest);
            }

            if (particle.StartsWith("으로") || particle.StartsWith("로"))
            {
                string rest = particle.StartsWith("으로") ? particle.Substring(2) : particle.Substring(1);
                if (hasCoda && jongseong != 8)
                    return "으로" + rest;
                else
                    return "로" + rest;
            }

            return particle;
        }

        private static bool IsStandaloneWordAfterLink(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return s.StartsWith("모두") || s.StartsWith("만세") || s.StartsWith("말이오") ||
                   s.StartsWith("중위") || s.StartsWith("정부") || s.StartsWith("및") ||
                   s == "중" || s.StartsWith("측") || s.StartsWith("소속") || s.StartsWith("출신");
        }

        private static string ResolveParentheticalParticles(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('(') < 0) return text;
            return _parenParticleRegex.Replace(text, m =>
            {
                string word = m.Groups["word"].Value;
                string tags = m.Groups["tags"].Value;
                if (m.Groups["p1"].Success && m.Groups["p1"].Length > 0)
                {
                    string p1 = m.Groups["p1"].Value;
                    string p2 = m.Groups["p2"].Value;
                    string chosen = FixKoreanParticle(word, "(" + p1 + ")" + p2);
                    return word + tags + chosen;
                }
                else
                {
                    string alt1 = m.Groups["alt1"].Value;
                    string alt2 = m.Groups["alt2"].Value;
                    string chosen = FixKoreanParticle(word, alt1 + "(" + alt2 + ")");
                    return word + tags + chosen;
                }
            });
        }

        private static string GetTagKoreanLabel(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            string label;
            if (_tagMap != null && _tagMap.TryGetValue(key.Trim(), out label))
                return label;
            if (_translations != null && _translations.TryGetValue(key.Trim(), out label))
                return label;
            if (_translationsIgnoreCase != null && _translationsIgnoreCase.TryGetValue(key.Trim(), out label))
                return label;
            return key;
        }

        private static string ProcessDynamicTags(string trans, string defaultColor)
        {
            if (string.IsNullOrEmpty(trans) || trans.IndexOf("[[", StringComparison.Ordinal) < 0)
                return trans;

            trans = _tripleBracketRegex.Replace(trans, "[[$1]]");
            trans = _tripleOpenBracketRegex.Replace(trans, "[[$1]]");
            string col = defaultColor;
            return _bracketWithParticleRegex.Replace(trans, m =>
            {
                string key = m.Groups["key"].Value.Trim();
                string label = GetTagKoreanLabel(key);
                string space = m.Groups["space"].Success ? m.Groups["space"].Value : "";

                string linkHtml = "<color=" + col + "><link=\"" + key + "\">" + label + "</link></color>";

                if (m.Groups["particle"].Success && m.Groups["particle"].Length > 0)
                {
                    string rawParticle = m.Groups["particle"].Value;
                    if (!string.IsNullOrEmpty(space) || IsStandaloneWordAfterLink(rawParticle))
                    {
                        return linkHtml + " " + rawParticle;
                    }
                    string corrected = FixKoreanParticle(label, rawParticle);
                    return linkHtml + corrected;
                }

                return linkHtml + space;
            });
        }

        private static string TranslateColourizedLinks(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("<link=", StringComparison.OrdinalIgnoreCase) < 0)
                return text;

            // 1. <link="id"><color=#HEX><b>label</b></color></link>particle
            text = _linkColorOuterRegex.Replace(text, m =>
            {
                string id = m.Groups["id"].Value;
                string col = m.Groups["color"].Value;
                string b1 = m.Groups["b1"].Success ? m.Groups["b1"].Value : "";
                string b2 = m.Groups["b2"].Success ? m.Groups["b2"].Value : "";
                string label = m.Groups["label"].Value;
                string koLabel = GetTagKoreanLabel(id);
                if (koLabel == id && label != id) koLabel = GetTagKoreanLabel(label);

                string linkHtml = "<link=\"" + id + "\"><color=" + col + ">" + b1 + koLabel + b2 + "</color></link>";
                string space = m.Groups["space"].Success ? m.Groups["space"].Value : "";
                if (m.Groups["particle"].Success && m.Groups["particle"].Length > 0)
                {
                    string rawParticle = m.Groups["particle"].Value;
                    if (!string.IsNullOrEmpty(space) || IsStandaloneWordAfterLink(rawParticle))
                        return linkHtml + " " + rawParticle;
                    return linkHtml + FixKoreanParticle(koLabel, rawParticle);
                }
                return linkHtml + space;
            });

            // 2. <color=#HEX><link="id"><b>label</b></link></color>particle
            text = _linkColorInnerRegex.Replace(text, m =>
            {
                string id = m.Groups["id"].Value;
                string col = m.Groups["color"].Value;
                string b1 = m.Groups["b1"].Success ? m.Groups["b1"].Value : "";
                string b2 = m.Groups["b2"].Success ? m.Groups["b2"].Value : "";
                string label = m.Groups["label"].Value;
                string koLabel = GetTagKoreanLabel(id);
                if (koLabel == id && label != id) koLabel = GetTagKoreanLabel(label);

                string linkHtml = "<color=" + col + "><link=\"" + id + "\">" + b1 + koLabel + b2 + "</link></color>";
                string space = m.Groups["space"].Success ? m.Groups["space"].Value : "";
                if (m.Groups["particle"].Success && m.Groups["particle"].Length > 0)
                {
                    string rawParticle = m.Groups["particle"].Value;
                    if (!string.IsNullOrEmpty(space) || IsStandaloneWordAfterLink(rawParticle))
                        return linkHtml + " " + rawParticle;
                    return linkHtml + FixKoreanParticle(koLabel, rawParticle);
                }
                return linkHtml + space;
            });

            // 3. <link="id">label</link>particle (plain link without color yet)
            text = _linkPlainRegex.Replace(text, m =>
            {
                string id = m.Groups["id"].Value;
                string b1 = m.Groups["b1"].Success ? m.Groups["b1"].Value : "";
                string b2 = m.Groups["b2"].Success ? m.Groups["b2"].Value : "";
                string label = m.Groups["label"].Value;
                string koLabel = GetTagKoreanLabel(id);
                if (koLabel == id && label != id) koLabel = GetTagKoreanLabel(label);

                string linkHtml = "<link=\"" + id + "\">" + b1 + koLabel + b2 + "</link>";
                string space = m.Groups["space"].Success ? m.Groups["space"].Value : "";
                if (m.Groups["particle"].Success && m.Groups["particle"].Length > 0)
                {
                    string rawParticle = m.Groups["particle"].Value;
                    if (!string.IsNullOrEmpty(space) || IsStandaloneWordAfterLink(rawParticle))
                        return linkHtml + " " + rawParticle;
                    return linkHtml + FixKoreanParticle(koLabel, rawParticle);
                }
                return linkHtml + space;
            });

            return text;
        }

        private static string FixKnownParticleGlitches(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = text.Replace("시간의 신들가", "시간의 신들이");
            text = text.Replace("시간의 신는", "시간의 신은");
            text = text.Replace("시간의 신들는", "시간의 신들은");
            text = text.Replace("시간의 신라면", "시간의 신이라면");
            text = text.Replace("나날의 대장간라", "나날의 대장간이라");
            text = text.Replace("날들의 대장간라", "나날의 대장간이라");
            text = text.Replace("비세속적 기술가", "비세속적 기술이");
            text = text.Replace("기술가가", "기술이");
            text = text.Replace("기술 가", "기술이");
            text = text.Replace("기술가", "기술이");
            text = text.Replace("흔적 가", "흔적이");
            text = text.Replace("흔적가", "흔적이");
            text = text.Replace("원리은", "원리는");
            text = text.Replace("성배-원리은", "성배-원리는");

            return text;
        }

        private static string PostProcessResult(string text, string defaultColor)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string res = text;
            if (res.IndexOf("[[", StringComparison.Ordinal) >= 0)
            {
                res = ProcessDynamicTags(res, defaultColor);
            }
            if (res.IndexOf("<link=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                res = TranslateColourizedLinks(res);
            }
            if (res.IndexOf('(') >= 0)
            {
                res = ResolveParentheticalParticles(res);
            }
            return FixKnownParticleGlitches(res);
        }

        private static string TranslateBracketPrefixes(string bracketsPart)
        {
            if (string.IsNullOrEmpty(bracketsPart)) return bracketsPart;
            return _singleBracketRegex.Replace(bracketsPart, m =>
            {
                string inner = m.Groups["inner"].Value.Trim();
                if (inner.IndexOf(';') >= 0)
                {
                    string[] parts = inner.Split(';');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string p = parts[i].Trim();
                        parts[i] = TranslateSingleTagOrText(p);
                    }
                    return "[" + string.Join("; ", parts) + "]";
                }
                string tr = TranslateSingleTagOrText(inner);
                return "[" + tr + "]";
            });
        }

        private static string TranslateSingleTagOrText(string inner)
        {
            if (string.IsNullOrEmpty(inner)) return inner;
            inner = inner.Trim().Replace("’", "'").Replace("‘", "'");

            if (string.Equals(inner, "<u>Success</u>", StringComparison.OrdinalIgnoreCase)) return "<u>성공</u>";
            if (string.Equals(inner, "<u>Failure</u>", StringComparison.OrdinalIgnoreCase)) return "<u>실패</u>";

            string val;
            if (_tagMap != null && _tagMap.TryGetValue(inner, out val)) return val;
            if (_translations != null && _translations.TryGetValue(inner, out val)) return val;
            if (_translationsIgnoreCase != null && _translationsIgnoreCase.TryGetValue(inner, out val)) return val;
            if (_translations != null && _translations.TryGetValue("[" + inner + "]", out val))
            {
                if (val.StartsWith("[") && val.EndsWith("]") && val.Length > 2)
                    return val.Substring(1, val.Length - 2);
                return val;
            }
            string raw = LookupRaw(inner);
            if (raw != null) return raw;

            // Handle "Opinion - Linou - 대단히 호의적임" or "평판 - Linou - 대단히 호의적임"
            if (inner.StartsWith("Opinion - ", StringComparison.OrdinalIgnoreCase) || inner.StartsWith("평판 - "))
            {
                string rest = inner.StartsWith("Opinion - ", StringComparison.OrdinalIgnoreCase)
                    ? inner.Substring(10).Trim()
                    : inner.Substring(5).Trim();
                int dashIdx = rest.IndexOf(" - ");
                if (dashIdx > 0)
                {
                    string actor = rest.Substring(0, dashIdx).Trim();
                    string status = rest.Substring(dashIdx + 3).Trim();
                    string koActor = TranslateSpeakerName(actor);
                    if (koActor == actor) koActor = GetTagKoreanLabel(actor);
                    string koStatus = GetTagKoreanLabel(status);
                    return koActor + "에 대한 평판 - " + koStatus;
                }
                else
                {
                    string koActor = TranslateSpeakerName(rest);
                    if (koActor == rest) koActor = GetTagKoreanLabel(rest);
                    return koActor + "에 대한 평판";
                }
            }

            // Handle "<N> <Item>" e.g. "50 Francisques", "1 Deck of Playing Cards", "1 Silver Spintria", "1 Dread"
            var mLead = _qtyLeadingRegex.Match(inner);
            if (mLead.Success)
            {
                string num = mLead.Groups["num"].Value;
                string item = mLead.Groups["item"].Value.Trim();
                string koItem = GetTagKoreanLabel(item);
                if (koItem == item && item.EndsWith("s") && item.Length > 2)
                    koItem = GetTagKoreanLabel(item.Substring(0, item.Length - 1));
                if (koItem != item)
                {
                    if (koItem.EndsWith("들") && koItem.Length > 1)
                        koItem = koItem.Substring(0, koItem.Length - 1);
                    return koItem + " " + num;
                }
            }

            // Handle "<Item> <N>+" e.g. "Large Manila Envelope 1+", "Traces 1"
            var mTrail = _qtyTrailingRegex.Match(inner);
            if (mTrail.Success)
            {
                string item = mTrail.Groups["item"].Value.Trim();
                string num = mTrail.Groups["num"].Value.Trim();
                string koItem = GetTagKoreanLabel(item);
                if (koItem == item && item.EndsWith("s") && item.Length > 2)
                    koItem = GetTagKoreanLabel(item.Substring(0, item.Length - 1));
                if (koItem != item)
                {
                    if (koItem.EndsWith("들") && koItem.Length > 1)
                        koItem = koItem.Substring(0, koItem.Length - 1);
                    return koItem + " " + num;
                }
            }

            // Handle "No <Item>"
            var mNo = _noItemRegex.Match(inner);
            if (mNo.Success)
            {
                string item = mNo.Groups["item"].Value.Trim();
                string koItem = GetTagKoreanLabel(item);
                if (koItem != item)
                {
                    if (koItem.EndsWith("들") && koItem.Length > 1)
                        koItem = koItem.Substring(0, koItem.Length - 1);
                    return koItem + " 없음";
                }
            }

            // Handle comma-separated requirements like "Day, Night"
            if (inner.IndexOf(',') >= 0)
            {
                string[] cparts = inner.Split(',');
                bool anyChanged = false;
                for (int i = 0; i < cparts.Length; i++)
                {
                    string cp = cparts[i].Trim();
                    string trCp = GetTagKoreanLabel(cp);
                    if (trCp != cp) { cparts[i] = trCp; anyChanged = true; }
                }
                if (anyChanged) return string.Join(", ", cparts);
            }

            return inner;
        }

        public static string TranslateTokenValue(string val)
        {
            if (string.IsNullOrEmpty(val)) return val;
            if (!_initialized) Initialize();

            string trimmed = val.Trim();
            // Number words used by [q=chezfelix.bill:words]
            switch (trimmed.ToLowerInvariant())
            {
                case "zero": return "0";
                case "one": return "1";
                case "two": return "2";
                case "three": return "3";
                case "four": return "4";
                case "five": return "5";
                case "six": return "6";
                case "seven": return "7";
                case "eight": return "8";
                case "nine": return "9";
                case "ten": return "10";
                case "twelve": return "12";
                case "fifteen": return "15";
                case "sixteen": return "16";
                case "twenty": return "20";
                case "twenty-four": return "24";
                case "thirty": return "30";
                case "thirty-two": return "32";
                case "forty": return "40";
                case "forty-eight": return "48";
                case "fifty": return "50";
                case "sir": return "선생";
                case "my friend": return "벗";
                case "stranger": return "이방인";
                case "monsieur": return "무슈";
                case "herr": return "헤어";
                case "comrade": return "동무";
                case "camomile": return "카모마일";
                case "coca-cola": return "코카콜라";
                case "pastis": return "파스티스";
                case "red wine": return "레드 와인";
                case "white wine": return "화이트 와인";
                case "coffee": return "커피";
            }

            string spk = TranslateSpeakerName(trimmed);
            if (spk != trimmed) return spk;

            string tag = GetTagKoreanLabel(trimmed);
            if (tag != trimmed) return tag;

            return val;
        }

        private static readonly Regex _aspectSpriteRegex = new Regex(@"(<sprite=[^>]+>)?([A-Za-z0-9_-]+)", RegexOptions.Compiled);

        public static string TranslateAspectPhrase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            // Replace conjunctions
            string res = s.Replace(" or ", " 또는 ").Replace(" and ", " 및 ");

            res = _aspectSpriteRegex.Replace(res, m =>
            {
                string sprite = m.Groups[1].Value;
                string name = m.Groups[2].Value.Trim();
                if (string.IsNullOrEmpty(name)) return m.Value;
                if (name == "또는" || name == "및") return m.Value;

                string ko = GetTagKoreanLabel(name);
                if (ko != name)
                {
                    return sprite + ko;
                }
                string tr = LookupRaw(name);
                if (tr != null && tr != name)
                {
                    return sprite + tr;
                }
                return m.Value;
            });

            return res;
        }

        private static string TranslateCommaSeparatedAspects(string aspectsStr)
        {
            if (string.IsNullOrEmpty(aspectsStr)) return aspectsStr;
            string[] parts = aspectsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                var mNum = _qtyLeadingRegex.Match(p);
                if (mNum.Success)
                {
                    string num = mNum.Groups["num"].Value;
                    string asp = mNum.Groups["item"].Value.Trim();
                    string koAsp = GetTagKoreanLabel(asp);
                    parts[i] = koAsp + " " + num;
                }
                else
                {
                    parts[i] = GetTagKoreanLabel(p);
                }
            }
            return string.Join(", ", parts);
        }

        public static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!_initialized) Initialize();
            if (_translations == null || _translations.Count == 0) return text;

            try
            {
                // 0-A. Unwrap inline star alert: <i><sprite="inline" name="star"> ...</i>
                var mStar = _inlineStarRegex.Match(text);
                if (mStar.Success)
                {
                    string innerStar = mStar.Groups["inner"].Value;
                    string trInner = Translate(innerStar);
                    return mStar.Groups["pre"].Value + trInner + mStar.Groups["post"].Value;
                }

                // 0-B. Unwrap Journal red bar: <uppercase>TITLE</uppercase> -- DESC
                var mRedBar = _journalRedBarRegex.Match(text);
                if (mRedBar.Success)
                {
                    string rTitle = mRedBar.Groups["title"].Value.Trim();
                    string rDesc = mRedBar.Groups["desc"].Value.Trim();
                    string koTitle = Translate(rTitle);
                    if (koTitle == rTitle) koTitle = GetTagKoreanLabel(rTitle);
                    string koDesc = Translate(rDesc);
                    return "<uppercase>" + koTitle + "</uppercase> -- " + koDesc;
                }

                bool hasLinkTag = text.IndexOf("<link=", StringComparison.OrdinalIgnoreCase) >= 0;
                string lookupText = text;
                string defaultColor = "#D24B00";

                if (hasLinkTag)
                {
                    var match = _linkColorInnerRegex.Match(text);
                    if (match.Success && match.Groups["color"].Success)
                    {
                        defaultColor = match.Groups["color"].Value;
                    }
                    else
                    {
                        var match2 = _linkColorOuterRegex.Match(text);
                        if (match2.Success && match2.Groups["color"].Success)
                        {
                            defaultColor = match2.Groups["color"].Value;
                        }
                    }
                    lookupText = _linkColorInnerRegex.Replace(lookupText, m => "[[" + m.Groups["id"].Value + "]]" + (m.Groups["space"].Success ? m.Groups["space"].Value : "") + (m.Groups["particle"].Success ? m.Groups["particle"].Value : ""));
                    lookupText = _linkColorOuterRegex.Replace(lookupText, m => "[[" + m.Groups["id"].Value + "]]" + (m.Groups["space"].Success ? m.Groups["space"].Value : "") + (m.Groups["particle"].Success ? m.Groups["particle"].Value : ""));
                    lookupText = _linkPlainRegex.Replace(lookupText, m => "[[" + m.Groups["id"].Value + "]]" + (m.Groups["space"].Success ? m.Groups["space"].Value : "") + (m.Groups["particle"].Success ? m.Groups["particle"].Value : ""));
                }

                // 1. Direct or normalized lookup
                string trans = LookupRaw(lookupText);
                if (trans != null)
                {
                    return PostProcessResult(trans, defaultColor);
                }

                // 1-B. Unwrap <size=...>INNER</size> or <cspace=...>INNER</cspace>
                var mSize = _sizeWrapRegex.Match(text);
                if (mSize.Success)
                {
                    string innerSz = mSize.Groups["inner"].Value;
                    string trSz = Translate(innerSz);
                    if (trSz != innerSz)
                        return mSize.Groups["open"].Value + trSz + mSize.Groups["close"].Value;
                }

                // 2. Check speaker name
                string spk = TranslateSpeakerName(text);
                if (spk != text) return spk;

                // 3. Check quote format: "— Author, Work"
                var qm = _quoteSourceRegex.Match(text);
                if (qm.Success)
                {
                    string auth = qm.Groups["author"].Value.Trim();
                    string work = qm.Groups["work"].Value.Trim().Replace("<i>", "").Replace("</i>", "").Trim('《', '》', ' ');
                    string authKo = GetTagKoreanLabel(auth);
                    if (authKo == auth) authKo = LookupRaw(auth) ?? auth;
                    string workKo = GetTagKoreanLabel(work);
                    if (workKo == work) workKo = LookupRaw(work) ?? work;
                    workKo = workKo.TrimStart('《').TrimEnd('》');
                    return "— " + authKo + ", 《" + workKo + "》";
                }

                // 4. Check bracket prefixes: e.g. "[끝][Craft] ...", "[Not Ready For Crafting?] [이곳에 무언가 더 있다...]", "1. [끝][Craft] ..."
                var bm = _multiBracketPrefixRegex.Match(lookupText);
                if (bm.Success)
                {
                    string numPart = bm.Groups["num"].Success ? bm.Groups["num"].Value : "";
                    string bracketsPart = bm.Groups["brackets"].Value;
                    string bodyPart = bm.Groups["body"].Value.Trim();

                    string trBrackets = TranslateBracketPrefixes(bracketsPart.Trim());
                    trBrackets = trBrackets.Replace("][", "] [");

                    string trBody = bodyPart;
                    if (!string.IsNullOrEmpty(bodyPart))
                    {
                        string bodyLookup = LookupRaw(bodyPart);
                        if (bodyLookup != null)
                        {
                            trBody = bodyLookup;
                        }
                        else if (bodyPart.StartsWith("[") && bodyPart.EndsWith("]"))
                        {
                            trBody = TranslateBracketPrefixes(bodyPart);
                        }
                        else
                        {
                            trBody = Translate(bodyPart);
                        }
                    }

                    string combined = numPart + trBrackets + (string.IsNullOrEmpty(trBody) ? "" : (" " + trBody));
                    return PostProcessResult(combined, defaultColor);
                }

                // 5. Dynamic UI patterns (Skill modifiers, SmolSkillImages picker, vs checks, Companion bonuses, Aspect preferences, Shop truncation)
                // 5-A. SmolSkillImages picker: <sprite="SmolSkillImages"...> Birdsong <color=#808080>[10%] → </color> [10%]
                var mSmol = _smolSkillPickerRegex.Match(text);
                if (mSmol.Success)
                {
                    string sk = mSmol.Groups["skill"].Value.Trim();
                    string koSk = GetTagKoreanLabel(sk);
                    return mSmol.Groups["pre"].Value + koSk + mSmol.Groups["post"].Value;
                }

                // 5-B. Skill modifier: "Observation +2" or "<i>Eloquence +2</i>"
                var mSkMod = _skillModRegex.Match(text);
                if (mSkMod.Success)
                {
                    string sk = mSkMod.Groups["skill"].Value.Trim();
                    string koSk = GetTagKoreanLabel(sk);
                    if (koSk != sk)
                    {
                        string i1 = mSkMod.Groups["i1"].Success ? mSkMod.Groups["i1"].Value : "";
                        string i2 = mSkMod.Groups["i2"].Success ? mSkMod.Groups["i2"].Value : "";
                        return i1 + koSk + " " + mSkMod.Groups["mod"].Value + i2;
                    }
                }

                // 5-C. UnusedSkillCheckLineFormatter: "Bosk +0 vs 10" or "Eloquence +0 vs 10;Birdsong +0 vs 10"
                if (text.IndexOf(" vs ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string replacedVs = _skillVsRegex.Replace(text, m =>
                    {
                        string sk = m.Groups["skill"].Value.Trim();
                        string koSk = GetTagKoreanLabel(sk);
                        return koSk + " " + m.Groups["mod"].Value + " 대 " + m.Groups["diff"].Value;
                    });
                    if (replacedVs != text)
                    {
                        return replacedVs.Replace(";", "; ");
                    }
                }

                // 5-D. Companion assistance or "Skill: +2" list
                if (text.IndexOf(": +", StringComparison.Ordinal) >= 0 || text.IndexOf(": -", StringComparison.Ordinal) >= 0)
                {
                    string replacedComp = _companionModRegex.Replace(text, m =>
                    {
                        string sk = m.Groups["skill"].Value.Trim();
                        string koSk = GetTagKoreanLabel(sk);
                        return koSk + ": " + m.Groups["mod"].Value;
                    });
                    if (replacedComp != text)
                        return PostProcessResult(replacedComp, defaultColor);
                }

                // 5-E. Aspect preference: "+3 좋아함: Raffish" or "싫어함: Formal"
                if (text.IndexOf("좋아함:", StringComparison.Ordinal) >= 0 || text.IndexOf("싫어함:", StringComparison.Ordinal) >= 0)
                {
                    string replacedPref = _aspectPrefRegex.Replace(text, m =>
                    {
                        string asp = m.Groups["aspect"].Value.Trim();
                        return m.Groups["prefix"].Value + GetTagKoreanLabel(asp);
                    });
                    if (replacedPref != text) return replacedPref;
                }

                // 5-F. Crafting slot & Aspect pool recovery with English aspect names inside Korean template
                if (text.IndexOf("지닌 재료를 추가하세요", StringComparison.Ordinal) >= 0)
                {
                    string innerText = text;
                    bool italic = innerText.StartsWith("<i>") && innerText.EndsWith("</i>");
                    if (italic) innerText = innerText.Substring(3, innerText.Length - 7);
                    int idxStart = innerText.IndexOf("속성 ");
                    int idxEnd = innerText.IndexOf(" 지닌 재료를 추가하세요");
                    if (idxStart >= 0 && idxEnd > idxStart + 3)
                    {
                        string mid = innerText.Substring(idxStart + 3, idxEnd - (idxStart + 3));
                        string particlePart = "(을)를";
                        if (mid.EndsWith("(을)를")) mid = mid.Substring(0, mid.Length - 4);
                        else if (mid.EndsWith("을") || mid.EndsWith("를")) { particlePart = mid.Substring(mid.Length - 1); mid = mid.Substring(0, mid.Length - 1); }
                        string koMid = TranslateCommaSeparatedAspects(mid);
                        string rebuilt = innerText.Substring(0, idxStart + 3) + koMid + FixKoreanParticle(koMid, particlePart) + innerText.Substring(idxEnd);
                        return italic ? ("<i>" + rebuilt + "</i>") : rebuilt;
                    }
                }
                if (text.IndexOf("속성 풀이 ", StringComparison.Ordinal) >= 0 && text.IndexOf("만큼 회복되었습니다", StringComparison.Ordinal) >= 0)
                {
                    int idxStart = text.IndexOf("속성 풀이 ") + 6;
                    int idxEnd = text.IndexOf("만큼 회복되었습니다");
                    if (idxEnd > idxStart)
                    {
                        string mid = text.Substring(idxStart, idxEnd - idxStart);
                        string koMid = TranslateCommaSeparatedAspects(mid);
                        return text.Substring(0, idxStart) + koMid + text.Substring(idxEnd);
                    }
                }

                // 5-G. Shop truncated item label ending with '…' (e.g. "GRENADINE CLARET…")
                if (text.EndsWith("…") && text.Length > 3)
                {
                    string prefix = text.Substring(0, text.Length - 1).Trim();
                    foreach (var kv in _tagMap)
                    {
                        if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            return kv.Value;
                        }
                    }
                    foreach (var kv in _translations)
                    {
                        if (kv.Key.Length < 80 && kv.Key.IndexOf('\n') < 0 && kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            return kv.Value;
                        }
                    }
                }

                // 6. Process any [[tags]], <link="tags">, and (이)가 / 이(가) particles!
                return PostProcessResult(text, defaultColor);
            }
            catch
            {
                return text;
            }
        }

        public static string TranslateDialogue(string text)
        {
            return Translate(text);
        }

        public static string TranslateDialogueResponse(string text)
        {
            return Translate(text);
        }

        public static string PreprocessTooltipText(string text)
        {
            return Translate(text);
        }

        private static string LookupRaw(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            string trans;
            if (_translations.TryGetValue(text, out trans))
            {
                return trans;
            }

            string trimmed = text.Trim();
            if (_translations.TryGetValue(trimmed, out trans))
            {
                return trans;
            }

            if (_translationsIgnoreCase.TryGetValue(trimmed, out trans))
            {
                return trans;
            }

            string normalized = CollapseWhitespace(text);
            if (_normalizedMap.TryGetValue(normalized, out trans))
            {
                return trans;
            }

            if (text.IndexOf('’') >= 0 || text.IndexOf('‘') >= 0)
            {
                string straight = text.Replace("’", "'").Replace("‘", "'");
                if (_translations.TryGetValue(straight, out trans) || _translationsIgnoreCase.TryGetValue(straight.Trim(), out trans))
                    return trans;
            }

            return null;
        }

        private static string CollapseWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return _wsRegex.Replace(input.Trim(), " ").ToLowerInvariant();
        }

        public static string TranslateLocKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!_initialized) Initialize();

            string val;
            if (_locKeyMap != null && _locKeyMap.TryGetValue(key, out val))
            {
                return val;
            }
            if (_translations != null && _translations.TryGetValue(key, out val))
            {
                return val;
            }
            return null;
        }

        public static void TranslateLocArgs(ref string template, string[] args)
        {
            if (string.IsNullOrEmpty(template) && (args == null || args.Length == 0)) return;
            if (!_initialized) Initialize();

            if (!string.IsNullOrEmpty(template))
            {
                string transTmpl = Translate(template);
                if (transTmpl != null && transTmpl != template)
                    template = transTmpl;
            }

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (string.IsNullOrEmpty(arg)) continue;

                    // 1. Handle [[Tag]] inside args (e.g. "[[Fascination]]", "[[Weariness]]" from Skill::GetAdvice)
                    if (arg.StartsWith("[[") && arg.EndsWith("]]") && arg.Length > 4)
                    {
                        string inner = arg.Substring(2, arg.Length - 4).Trim();
                        string koLabel = GetTagKoreanLabel(inner);
                        args[i] = "<link=\"" + inner + "\">" + koLabel + "</link>";
                        continue;
                    }

                    // 2. ALWAYS check full tag / full translation lookup FIRST before splitting on commas!
                    string fullTagKo = GetTagKoreanLabel(arg);
                    if (fullTagKo != arg)
                    {
                        args[i] = PostProcessResult(fullTagKo, "#D24B00");
                        continue;
                    }
                    string fullTr = LookupRaw(arg);
                    if (fullTr != null && fullTr != arg)
                    {
                        args[i] = PostProcessResult(fullTr, "#D24B00");
                        continue;
                    }

                    // 3. Handle aspect phrases containing <sprite=...>, " or ", or slashes
                    if (arg.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        arg.IndexOf(" or ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (arg.IndexOf('/') >= 0 && !arg.StartsWith("http")))
                    {
                        args[i] = TranslateAspectPhrase(arg);
                        continue;
                    }

                    // 4. Only if not found as a whole string, check if it's a comma-separated list of aspects/skills/items
                    if (arg.IndexOf(',') >= 0)
                    {
                        args[i] = TranslateCommaSeparatedAspects(arg);
                    }
                    else
                    {
                        string tr = Translate(arg);
                        if (tr != null && tr != arg)
                        {
                            args[i] = tr;
                        }
                    }
                }
            }
        }

        public static void HookQuoteScene(TMP_Text quoteText, TMP_Text sourceText)
        {
            if (!_initialized) Initialize();

            if (quoteText != null)
            {
                EnsureTMPText(quoteText);
                string tr = Translate(quoteText.text);
                if (!string.IsNullOrEmpty(tr) && tr != quoteText.text)
                {
                    quoteText.text = tr;
                }
            }

            if (sourceText != null)
            {
                EnsureTMPText(sourceText);
                string tr = Translate(sourceText.text);
                if (!string.IsNullOrEmpty(tr) && tr != sourceText.text)
                {
                    sourceText.text = tr;
                }
            }
        }

        public static string TranslateSpeakerName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            if (!_initialized) Initialize();

            string prefix = "";
            string name = raw;

            if (raw.StartsWith("<sprite=", StringComparison.OrdinalIgnoreCase))
            {
                int closeIdx = raw.IndexOf('>');
                if (closeIdx >= 0)
                {
                    prefix = raw.Substring(0, closeIdx + 1);
                    name = raw.Substring(closeIdx + 1);
                }
            }

            string trimmed = name.Trim();
            bool hasBullet = false;
            if (trimmed.StartsWith("•") || trimmed.StartsWith("·"))
            {
                hasBullet = true;
                trimmed = trimmed.Substring(1).Trim();
            }

            string suffix = "";
            if (trimmed.EndsWith("_") || trimmed.EndsWith("-"))
            {
                suffix = trimmed.Substring(trimmed.Length - 1);
                trimmed = trimmed.Substring(0, trimmed.Length - 1).Trim();
            }

            string translated = null;
            if (!_speakerMap.TryGetValue(trimmed, out translated))
            {
                return raw;
            }

            string result = translated;
            if (hasBullet) result = "•" + result;
            if (!string.IsNullOrEmpty(suffix)) result = result + suffix;

            return prefix + result;
        }

        private static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
        }

        private static void InitSpeakerMap()
        {
            _speakerMap["Me"] = "당신";
            _speakerMap["Player"] = "당신";
            _speakerMap["Andrée"] = "안드레";
            _speakerMap["Aristide"] = "아리스티드";
            _speakerMap["Arun"] = "아룬";
            _speakerMap["Bernadette"] = "베르나데트";
            _speakerMap["Bessy"] = "베시";
            _speakerMap["Bronek"] = "브로네크";
            _speakerMap["Brother Cenacle"] = "세나클 수사";
            _speakerMap["Captain Torgue"] = "토르그 선장";
            _speakerMap["Chaima"] = "샤이마";
            _speakerMap["Christopher"] = "크리스토퍼";
            _speakerMap["Corso"] = "코르소";
            _speakerMap["D.O. Moore"] = "D.O. 무어";
            _speakerMap["Dagmar"] = "다그마르";
            _speakerMap["Dr Aubière"] = "오비에르 박사";
            _speakerMap["Dr Aubiere"] = "오비에르 박사";
            _speakerMap["Dr J.N. Sinombre"] = "시놈브레 박사";
            _speakerMap["Dr Sinombre"] = "시놈브레 박사";
            _speakerMap["Edouarde"] = "에두아르";
            _speakerMap["Eustache"] = "외스타슈";
            _speakerMap["Father Jaume"] = "조메 신부";
            _speakerMap["Floris"] = "플로리스";
            _speakerMap["Fraser Strathcoyne"] = "프레이저 스트래스코인";
            _speakerMap["Frauke"] = "프라우케";
            _speakerMap["Goupil"] = "구필";
            _speakerMap["Grimbert"] = "그림베르";
            _speakerMap["Hermine"] = "에르민";
            _speakerMap["Hokobald"] = "호코발트";
            _speakerMap["Isabeau"] = "이자보";
            _speakerMap["Jean-Bernard"] = "장베르나르";
            _speakerMap["Khione"] = "키오네";
            _speakerMap["Lazare"] = "라자르";
            _speakerMap["Leon"] = "레옹";
            _speakerMap["Leopold"] = "레오폴드";
            _speakerMap["Liliane Huissier"] = "릴리안 위시에";
            _speakerMap["Linou"] = "리누";
            _speakerMap["Mabelie"] = "마벨리";
            _speakerMap["Mathis"] = "마티스";
            _speakerMap["Maître Argant"] = "아르강 공증인";
            _speakerMap["Maitre Argant"] = "아르강 공증인";
            _speakerMap["Medea"] = "메데이아";
            _speakerMap["Melancthe"] = "멜랑트";
            _speakerMap["Mme Zélia"] = "젤리아 부인";
            _speakerMap["Mme Zèlia"] = "젤리아 부인";
            _speakerMap["Mme Zelia"] = "젤리아 부인";
            _speakerMap["Monica"] = "모니카";
            _speakerMap["Mother Mamerta"] = "마메르타 원장 수녀";
            _speakerMap["Nina"] = "니나";
            _speakerMap["Nine"] = "나인";
            _speakerMap["Olimpia"] = "올림피아";
            _speakerMap["Phil"] = "필";
            _speakerMap["Philomèle"] = "필로멜";
            _speakerMap["Philomele"] = "필로멜";
            _speakerMap["Pithcairn"] = "핏케언";
            _speakerMap["Pompon"] = "퐁퐁";
            _speakerMap["Rodia Vlachou"] = "로디아 블라후";
            _speakerMap["Sidonie"] = "시도니";
            _speakerMap["Sister Pancrata"] = "판크라타 수녀";
            _speakerMap["Sister Servata"] = "세르바타 수녀";
            _speakerMap["Sister Sophia"] = "소피아 수녀";
            _speakerMap["Surveillante Odette"] = "오데트 감독관";
            _speakerMap["Teresa"] = "테레사";
            _speakerMap["The elder Janvier"] = "장비에 노인";
            _speakerMap["Vincent"] = "뱅상";
            _speakerMap["Xamanek"] = "샤마네크";
            _speakerMap["Advice"] = "조언";
            _speakerMap["Telephone"] = "전화기";
            _speakerMap["Safe"] = "금고";
            _speakerMap["The Dead"] = "죽은 자들";
            _speakerMap["Wanderer"] = "방랑자";
            _speakerMap["Joyous Remnant"] = "환희의 잔재";
            _speakerMap["Pale Remnant"] = "창백한 잔재";
            _speakerMap["Perfumed Remnant"] = "향기로운 잔재";
            _speakerMap["Skin of the World"] = "세계의 피부";
            _speakerMap["Sticky Widow"] = "끈적거리는 과부";
            _speakerMap["Earthly Powers"] = "세속의 권력";
            _speakerMap["Fire in Darkness"] = "어둠 속의 불";
            _speakerMap["Wonderful Things"] = "경이로운 것들";
            _speakerMap["Beautiful Endings"] = "아름다운 결말";
            _speakerMap["Zoarium"] = "조아리움";
            _speakerMap["Encore"] = "앙코르";
            _speakerMap["[Reading]"] = "[독서 중]";
            _speakerMap["Huddled Form"] = "웅크린 형체";
            _speakerMap["Burly Type"] = "건장한 사내";
            _speakerMap["Languid Girl"] = "나른한 소녀";
            _speakerMap["Imposing Nun"] = "위엄 있는 수녀";
            _speakerMap["Cheery Nun"] = "쾌활한 수녀";
            _speakerMap["Wistful Nun"] = "수심 어린 수녀";
            _speakerMap["Something on the telephone"] = "전화기 너머의 무언가";
            _speakerMap["Motley Captain"] = "잡색 옷의 선장";
            _speakerMap["Astorre"] = "아스토레";
            _speakerMap["Bébette"] = "베베트";
            _speakerMap["Bebette"] = "베베트";
            _speakerMap["Bègue"] = "베그";
            _speakerMap["Begue"] = "베그";
            _speakerMap["Polchinelle"] = "폴치넬레";
            _speakerMap["Élise"] = "엘리즈";
            _speakerMap["Elise"] = "엘리즈";
            _speakerMap["Cassandro"] = "카산드로";
            _speakerMap["Scapin"] = "스카팽";
            _speakerMap["Valère"] = "발레르";
            _speakerMap["Valere"] = "발레르";
            _speakerMap["Metaphraste"] = "메타프라스트";
            _speakerMap["Mezzetin"] = "메즈탱";
            _speakerMap["Ghirbi"] = "기르비";
            _speakerMap["Sister Pancrata"] = "판크라타 수녀";
            _speakerMap["Sister Bernadette"] = "베르나데트 수녀";
            _speakerMap["Aunt Mopsy"] = "몹시 이모";
            _speakerMap["Chi"] = "치";
            _speakerMap["Torgue"] = "토르그";
        }

        private static void InitTagMap()
        {
            _tagMap["Respectable"] = "점잖음";
            _tagMap["Louche"] = "퇴폐적임";
            _tagMap["Formal"] = "격식 있음";
            _tagMap["Light"] = "가벼움";
            _tagMap["Abbey of St Brandan"] = "성 브랜던 수도원";
            _tagMap["Amiral"] = "다르랑 제독";
            _tagMap["Aviform"] = "조형(鳥形)";
            _tagMap["Beach-Crow"] = "해변 까마귀";
            _tagMap["Bonanza Cigarettes"] = "보난자 담배";
            _tagMap["Bureau"] = "억제국";
            _tagMap["Suppression Bureau"] = "억제국";
            _tagMap["Church Solar"] = "태양 교회";
            _tagMap["Coseley"] = "코슬리";
            _tagMap["Cross"] = "십자가회";
            _tagMap["Duties"] = "직무";
            _tagMap["Elegiast"] = "비가인";
            _tagMap["Forge of Days"] = "나날의 대장간";
            _tagMap["Gleam"] = "글림";
            _tagMap["Gods-who-were-flesh"] = "육신이었던 신들";
            _tagMap["Gwendolen"] = "궨돌린";
            _tagMap["Henades"] = "헤나드들";
            _tagMap["henades"] = "헤나드들";
            _tagMap["Henad"] = "헤나드";
            _tagMap["henad"] = "헤나드";
            _tagMap["Hour"] = "시간의 신";
            _tagMap["Hours"] = "시간의 신들";
            _tagMap["Icarian"] = "이카리아";
            _tagMap["Illumination"] = "조명술";
            _tagMap["Incorporate"] = "법인 연합";
            _tagMap["Know"] = "안 자";
            _tagMap["Lionsmith"] = "사자대장장이";
            _tagMap["Long"] = "장생자";
            _tagMap["long"] = "장생자";
            _tagMap["Légion"] = "군단";
            _tagMap["Legion"] = "군단";
            _tagMap["Légion du Seuil"] = "문턱 군단";
            _tagMap["Legion du Seuil"] = "문턱 군단";
            _tagMap["Marches"] = "변방";
            _tagMap["Ministries"] = "사역부";
            _tagMap["Name"] = "명명자";
            _tagMap["Names"] = "명명자들";
            _tagMap["Nowhere"] = "무위";
            _tagMap["Outfit"] = "의상";
            _tagMap["Outfits"] = "의상";
            _tagMap["Sign"] = "표식";
            _tagMap["Signs"] = "표식";
            _tagMap["Sister-and-Witch"] = "자매와 마녀";
            _tagMap["Sister-And-Witch"] = "자매와 마녀";
            _tagMap["Witch-and-Sister"] = "마녀와 자매";
            _tagMap["Witch-And-Sister"] = "마녀와 자매";
            _tagMap["Stag Door"] = "수사슴의 문";
            _tagMap["Thunderskin"] = "천둥가죽";
            _tagMap["Trace"] = "흔적";
            _tagMap["Traces"] = "흔적";
            _tagMap["Trouble"] = "곤경";
            _tagMap["Troubles"] = "곤경";
            _tagMap["Uncounted"] = "헤아려지지 않는 자";
            _tagMap["Venue"] = "제작 장소";
            _tagMap["Wake"] = "각성세계";
            _tagMap["Wars"] = "전쟁";
            _tagMap["Watchman"] = "성시자";
            _tagMap["Watchman's Tree"] = "성시자의 나무";
            _tagMap["Watchman’s Tree"] = "성시자의 나무";
            _tagMap["Wolf Divided"] = "분열된 늑대";
            _tagMap["Worm"] = "벌레";
            _tagMap["Worms"] = "벌레들";
            _tagMap["francisque"] = "프랑시스크";
            _tagMap["Francisque"] = "프랑시스크";
            _tagMap["little joys of night"] = "밤의 작은 기쁨들";
            _tagMap["remortals"] = "필멸회귀자";
            _tagMap["Remortals"] = "필멸회귀자";
            _tagMap["remortal"] = "필멸회귀자";
            _tagMap["Knock"] = "두드림";
            _tagMap["Knocks"] = "두드림들";
            _tagMap["Knock Experiences"] = "두드림 경험";
            _tagMap["Knock Memory"] = "두드림의 기억";
            _tagMap["Influence: Knock"] = "영향력: 두드림";
            _tagMap["Influence: Knocks"] = "영향력: 두드림들";
            _tagMap["Invisible Bag"] = "보이지 않는 가방";
            _tagMap["invisible bag"] = "보이지 않는 가방";
            _tagMap["Why?"] = "어째서입니까?";
            _tagMap["Obscure"] = "은폐";
            _tagMap["Bewitch"] = "홀림";
            _tagMap["Trespass"] = "침범";
            _tagMap["Expel"] = "추방";
            _tagMap["Entwine"] = "얽힘";
            _tagMap["Wither"] = "시듦";
            _tagMap["Quell"] = "진압";
            _tagMap["Unveil"] = "폭로";
            _tagMap["Concede"] = "양보";
            _tagMap["Moon"] = "달";
            _tagMap["Sky"] = "하늘";
            _tagMap["Scale"] = "비늘";
            _tagMap["Nectar"] = "감로";
            _tagMap["Abbey of St Brandan"] = "성 브랜던 수도원";
            _tagMap["Alexandria"] = "알렉산드리아";
            _tagMap["Allies"] = "연합국";
            _tagMap["Amiral"] = "다르랑 제독";
            _tagMap["Amiral Darlan"] = "다르랑 제독";
            _tagMap["Arts Unconsidered"] = "비세속의 기예";
            _tagMap["Aspect"] = "속성";
            _tagMap["Aspect Pool"] = "속성 풀";
            _tagMap["Aspects"] = "속성";
            _tagMap["Aviform"] = "조형(鳥形)";
            _tagMap["Beach-Crow"] = "해변 까마귀";
            _tagMap["Bewitch"] = "홀림";
            _tagMap["Blackwood"] = "블랙우드";
            _tagMap["Bonanza Cigarettes"] = "보난자 담배";
            _tagMap["Bosk"] = "숲";
            _tagMap["Brancrug"] = "브란크러그";
            _tagMap["Bright Art"] = "백색 기예";
            _tagMap["Bright Arts"] = "백색 기예";
            _tagMap["Bureau"] = "억제국";
            _tagMap["Cambray"] = "캉브레";
            _tagMap["Carapace Cross"] = "갑각 십자가회";
            _tagMap["Church"] = "교회";
            _tagMap["Church Solar"] = "태양 교회";
            _tagMap["Colonel"] = "대령";
            _tagMap["Corona"] = "코로나";
            _tagMap["Coseley"] = "코슬리";
            _tagMap["Cross"] = "십자가회";
            _tagMap["Crucian"] = "크루시안";
            _tagMap["Darlan"] = "다르랑";
            _tagMap["Dread"] = "공포";
            _tagMap["Duties"] = "직무";
            _tagMap["Ecdysis Club"] = "탈피회";
            _tagMap["Elegiast"] = "비가인";
            _tagMap["Enemy"] = "적";
            _tagMap["Entwine"] = "얽힘";
            _tagMap["Eternity"] = "영원";
            _tagMap["Experience"] = "경험";
            _tagMap["Experiences"] = "경험";
            _tagMap["Fascination"] = "매혹";
            _tagMap["Footnote"] = "각주";
            _tagMap["Footnotes"] = "각주";
            _tagMap["Forge"] = "대장간";
            _tagMap["Forge of Days"] = "나날의 대장간";
            _tagMap["Gladwyn Lake"] = "글래드윈 호수";
            _tagMap["Gleam"] = "글림";
            _tagMap["Glory"] = "영광";
            _tagMap["Gods-who-were-flesh"] = "육신이었던 신들";
            _tagMap["Great War"] = "대전쟁";
            _tagMap["Gwendolen"] = "궨돌린";
            _tagMap["Hallow"] = "축성";
            _tagMap["Henad"] = "헤나드";
            _tagMap["Henades"] = "헤나드들";
            _tagMap["Histories"] = "역사들";
            _tagMap["histories"] = "역사들";
            _tagMap["History"] = "역사";
            _tagMap["Horae"] = "시간들";
            _tagMap["Horned-Axe"] = "뿔 달린 도끼";
            _tagMap["Horomachistry"] = "시각학";
            _tagMap["Hour"] = "시간의 신";
            _tagMap["Hours"] = "시간의 신들";
            _tagMap["Huissier"] = "위시에";
            _tagMap["Hush House"] = "침묵의 서원";
            _tagMap["Hushery"] = "적막";
            _tagMap["Icarian"] = "이카리아";
            _tagMap["Icarianism"] = "이카리아주의";
            _tagMap["Icarians"] = "이카리아주의자들";
            _tagMap["Illumination"] = "조명술";
            _tagMap["Incorporate"] = "법인 연합";
            _tagMap["Incorporates"] = "법인 연합";
            _tagMap["Influence"] = "영향력";
            _tagMap["Influences"] = "영향력";
            _tagMap["Invisible Serapeum"] = "보이지 않는 세라페움";
            _tagMap["Ivory Dove"] = "상아 비둘기";
            _tagMap["Know"] = "지식";
            _tagMap["Lantern"] = "등불";
            _tagMap["Lark"] = "종달새";
            _tagMap["lark"] = "종달새";
            _tagMap["Lionsmith"] = "사자대장장이";
            _tagMap["Long"] = "롱";
            _tagMap["long"] = "롱";
            _tagMap["Légion"] = "군단";
            _tagMap["Légion du Seuil"] = "문턱 군단";
            _tagMap["Legion du Seuil"] = "문턱 군단";
            _tagMap["Madrugad"] = "마드루가드";
            _tagMap["Mansus"] = "만수스";
            _tagMap["Marches"] = "마르슈";
            _tagMap["Meniscate"] = "메니스케이트";
            _tagMap["Midnight Mark"] = "한밤의 표식";
            _tagMap["Ministries"] = "관구 성청";
            _tagMap["Moth"] = "나방";
            _tagMap["Name"] = "명명자";
            _tagMap["Names"] = "명명자들";
            _tagMap["Night Art"] = "밤의 기예";
            _tagMap["Night Arts"] = "밤의 기예";
            _tagMap["Nina Lagasse"] = "니나 라가스";
            _tagMap["Nowhere"] = "무(無)의 공간";
            _tagMap["Nyctodromy"] = "야행";
            _tagMap["OOC"] = "OOC";
            _tagMap["Outfit"] = "의상";
            _tagMap["Outfits"] = "복장";
            _tagMap["Pain"] = "고통";
            _tagMap["Passion"] = "열정";
            _tagMap["Passions"] = "열정";
            _tagMap["Pilgrims"] = "순례자들";
            _tagMap["pilgrim"] = "순례자";
            _tagMap["Preservation"] = "보존";
            _tagMap["Principle"] = "원리";
            _tagMap["Principles"] = "원리";
            _tagMap["Quell"] = "진압";
            _tagMap["Red Grail"] = "붉은 성배";
            _tagMap["Republic"] = "공화국";
            _tagMap["Serena"] = "세레나";
            _tagMap["Sign"] = "징표";
            _tagMap["Signs"] = "징표";
            _tagMap["Sister-and-Witch"] = "자매이자 마녀";
            _tagMap["Witch-and-Sister"] = "마녀이자 자매";
            _tagMap["Skill"] = "기술";
            _tagMap["Skills"] = "기술";
            _tagMap["Skolekosophy"] = "벌레학";
            _tagMap["Sophistication"] = "세련미";
            _tagMap["Stag Door"] = "수사슴의 문";
            _tagMap["Stanislav"] = "스타니슬라프";
            _tagMap["State"] = "국가";
            _tagMap["Sun"] = "태양";
            _tagMap["Sun-in-Rags"] = "누더기 태양";
            _tagMap["Suppression Bureau"] = "억제국";
            _tagMap["Thunderskin"] = "천둥가죽";
            _tagMap["Trace"] = "흔적";
            _tagMap["Traces"] = "흔적";
            _tagMap["Trouble"] = "곤경";
            _tagMap["Troubles"] = "곤경";
            _tagMap["Ubu"] = "위뷔";
            _tagMap["Uncounted"] = "헤아려지지 않는 자";
            _tagMap["Unveil"] = "폭로";
            _tagMap["Vagabond"] = "방랑자";
            _tagMap["Venue"] = "장소";
            _tagMap["Wake"] = "각성";
            _tagMap["Wars"] = "전쟁";
            _tagMap["Watchman"] = "간수";
            _tagMap["Watchman's Tree"] = "간수의 나무";
            _tagMap["Weariness"] = "피로";
            _tagMap["weariness"] = "피로";
            _tagMap["Wither"] = "시듦";
            _tagMap["Wolf Divided"] = "분열된 늑대";
            _tagMap["Wood"] = "숲";
            _tagMap["Worldly Skill"] = "세속 기술";
            _tagMap["Worldly Skills"] = "세속 기술";
            _tagMap["Worldly skill"] = "세속 기술";
            _tagMap["Worm"] = "웜";
            _tagMap["Worms"] = "웜들";
            _tagMap["Ys"] = "이스";
            _tagMap["currency"] = "통화";
            _tagMap["francisque"] = "프랑시스크";
            _tagMap["invisible bag"] = "보이지 않는 가방";
            _tagMap["little joys of night"] = "밤의 작은 기쁨들";
            _tagMap["remortals"] = "재필멸자들";
            _tagMap["seirai"] = "세이라이";
            _tagMap["sleep"] = "수면";
            _tagMap["the Barber's Tower"] = "이발사의 탑";
            _tagMap["travelling"] = "여행";
            _tagMap["links"] = "링크";
            _tagMap["Edge"] = "칼끝";
            _tagMap["Heart"] = "심장";
            _tagMap["Grail"] = "성배";
            _tagMap["Knock"] = "두드림";
            _tagMap["Rose"] = "장미";
            _tagMap["Winter"] = "겨울";
            _tagMap["Moon"] = "달";
            _tagMap["Nectar"] = "감로";
            _tagMap["Scale"] = "비늘";
            _tagMap["Sky"] = "하늘";
            _tagMap["Heart Relentless"] = "가차없는 심장";
            _tagMap["Chandler"] = "양초 장수";
            _tagMap["phost"] = "포스트";
            _tagMap["Charm"] = "매력";
            _tagMap["Birdsong"] = "새들의 노래";
            _tagMap["Dignity"] = "품위";
            _tagMap["Eloquence"] = "달변";
            _tagMap["Ithastry"] = "촛불학";
            _tagMap["Observation"] = "관찰";
            _tagMap["Spivvery"] = "암거래 수완";
            _tagMap["Stagecraft"] = "무대 연출";
            _tagMap["Sand-Dancing"] = "모래춤";
            _tagMap["Tristomancy"] = "비탄점술";
            _tagMap["Touch-Sense"] = "촉각감각";
            _tagMap["Crack-Sense"] = "균열감각";
            _tagMap["Iron-Sense"] = "철감각";
            _tagMap["Career"] = "경력";
            _tagMap["Item"] = "아이템";
            _tagMap["Items"] = "아이템";
            _tagMap["Uses"] = "횟수";
            _tagMap["uses"] = "횟수";
            _tagMap["Father Brass"] = "황동 신부";
            _tagMap["Heart's Desire?"] = "마음의 갈망?";
            _tagMap["Not Ready For Crafting?"] = "제작 준비 미흡?";
            _tagMap["Craft"] = "제작";
            _tagMap["Crafting"] = "제작";
            _tagMap["End"] = "끝";
            _tagMap["Back"] = "뒤로";
            _tagMap["Lie"] = "거짓말";
            _tagMap["Truth"] = "진실";
            _tagMap["Abbey of St Brandan"] = "성 브랜던 수도원";
            _tagMap["Amiral"] = "다르랑 제독";
            _tagMap["Aviform"] = "조형(鳥形)";
            _tagMap["Beach-Crow"] = "해변 까마귀";
            _tagMap["Bewitch"] = "홀림";
            _tagMap["Bonanza Cigarettes"] = "보난자 담배";
            _tagMap["Bureau"] = "억제국";
            _tagMap["Church Solar"] = "태양 교회";
            _tagMap["Concede"] = "양보";
            _tagMap["Coseley"] = "코슬리";
            _tagMap["Cross"] = "십자가회";
            _tagMap["Duties"] = "직무";
            _tagMap["Elegiast"] = "비가인";
            _tagMap["Entwine"] = "얽힘";
            _tagMap["Expel"] = "추방";
            _tagMap["Forge of Days"] = "나날의 대장간";
            _tagMap["Formal"] = "격식 있음";
            _tagMap["Francisque"] = "프랑시스크";
            _tagMap["Gleam"] = "글림";
            _tagMap["Gods-who-were-flesh"] = "육신이었던 신들";
            _tagMap["Gwendolen"] = "궨돌린";
            _tagMap["Henad"] = "헤나드";
            _tagMap["Henades"] = "헤나드들";
            _tagMap["Hour"] = "시간의 신";
            _tagMap["Hours"] = "시간의 신들";
            _tagMap["Icarian"] = "이카리아";
            _tagMap["Illumination"] = "조명술";
            _tagMap["Incorporate"] = "법인 연합";
            _tagMap["Influence: Knock"] = "영향력: 두드림";
            _tagMap["Influence: Knocks"] = "영향력: 두드림들";
            _tagMap["Invisible Bag"] = "보이지 않는 가방";
            _tagMap["Knock"] = "두드림";
            _tagMap["Knock Experiences"] = "두드림 경험";
            _tagMap["Knock Memory"] = "두드림의 기억";
            _tagMap["Knocks"] = "두드림들";
            _tagMap["Know"] = "안 자";
            _tagMap["Legion"] = "군단";
            _tagMap["Legion du Seuil"] = "문턱 군단";
            _tagMap["Light"] = "가벼움";
            _tagMap["Lionsmith"] = "사자대장장이";
            _tagMap["Long"] = "장생자";
            _tagMap["Louche"] = "퇴폐적임";
            _tagMap["Légion"] = "군단";
            _tagMap["Légion du Seuil"] = "문턱 군단";
            _tagMap["Marches"] = "변방";
            _tagMap["Ministries"] = "사역부";
            _tagMap["Moon"] = "달";
            _tagMap["Name"] = "명명자";
            _tagMap["Names"] = "명명자들";
            _tagMap["Nectar"] = "감로";
            _tagMap["Nowhere"] = "무위";
            _tagMap["Obscure"] = "은폐";
            _tagMap["Outfit"] = "의상";
            _tagMap["Outfits"] = "의상";
            _tagMap["Quell"] = "진압";
            _tagMap["Remortals"] = "필멸회귀자";
            _tagMap["Respectable"] = "점잖음";
            _tagMap["Scale"] = "비늘";
            _tagMap["Sign"] = "표식";
            _tagMap["Signs"] = "표식";
            _tagMap["Sister-And-Witch"] = "자매와 마녀";
            _tagMap["Sister-and-Witch"] = "자매와 마녀";
            _tagMap["Sky"] = "하늘";
            _tagMap["Stag Door"] = "수사슴의 문";
            _tagMap["Suppression Bureau"] = "억제국";
            _tagMap["Thunderskin"] = "천둥가죽";
            _tagMap["Trace"] = "흔적";
            _tagMap["Traces"] = "흔적";
            _tagMap["Trespass"] = "침범";
            _tagMap["Trouble"] = "곤경";
            _tagMap["Troubles"] = "곤경";
            _tagMap["Uncounted"] = "헤아려지지 않는 자";
            _tagMap["Unveil"] = "폭로";
            _tagMap["Venue"] = "제작 장소";
            _tagMap["Wake"] = "각성세계";
            _tagMap["Wars"] = "전쟁";
            _tagMap["Watchman"] = "성시자";
            _tagMap["Watchman's Tree"] = "성시자의 나무";
            _tagMap["Watchman’s Tree"] = "성시자의 나무";
            _tagMap["Why?"] = "어째서입니까?";
            _tagMap["Witch-And-Sister"] = "마녀와 자매";
            _tagMap["Witch-and-Sister"] = "마녀와 자매";
            _tagMap["Wither"] = "시듦";
            _tagMap["Wolf Divided"] = "분열된 늑대";
            _tagMap["Worm"] = "벌레";
            _tagMap["Worms"] = "벌레들";
            _tagMap["francisque"] = "프랑시스크";
            _tagMap["henad"] = "헤나드";
            _tagMap["henades"] = "헤나드들";
            _tagMap["invisible bag"] = "보이지 않는 가방";
            _tagMap["little joys of night"] = "밤의 작은 기쁨들";
            _tagMap["long"] = "장생자";
            _tagMap["remortal"] = "필멸회귀자";
            _tagMap["remortals"] = "필멸회귀자";
        }

        private static void InitLocKeyMap()
        {
            _locKeyMap["ADVICE_CAN_READ"] = "우클릭하여 읽습니다.";
            _locKeyMap["ADVICE_CAN_READ_ONCE"] = "우클릭하여 읽습니다 (1회 전용).";
            _locKeyMap["ADVICE_CAN_REFRESH_WITH"] = "1회용 아이템입니다. 우클릭하면 속성 풀의 {0} {1}(을)를 회복합니다 (아이템이 소모됩니다).";
            _locKeyMap["ADVICE_CAN_USE"] = "우클릭하여 사용합니다.";
            _locKeyMap["ADVICE_CAN_USE_ONCE"] = "1회용 아이템입니다. 우클릭하여 사용합니다.";
            _locKeyMap["ADVICE_CHANGED_ON_TRAVEL"] = "다른 도시로 이동하면 다른 물건으로 변합니다.";
            _locKeyMap["ADVICE_CONSUMED_ON_TRAVEL"] = "다른 도시로 이동하면 사라집니다.";
            _locKeyMap["ADVICE_EQUIPPED"] = "이 아이템은 활성 상태로 지정되어 기술 판정에 영향을 주고 있습니다.";
            _locKeyMap["ADVICE_HAS_SKILL_MODIFIERS"] = "활성 아이템 슬롯으로 드래그하세요.";
            _locKeyMap["ADVICE_HAS_SKILL_MODIFIERS_EXHAUSTABLE"] = "{0} 대화 선택지에서 이 1회용 아이템을 도움으로 선택할 수 있습니다. 활성화해 둘 필요는 없습니다.";
            _locKeyMap["ADVICE_IMPROVISABLE_ASPECT"] = "오컬트 작전입니다. 이 속성을 지닌 대화 선택지를 고르면 일치하는 아이템을 선택하여 기술 판정에 유리한 수정치를 받을 수 있습니다.";
            _locKeyMap["ADVICE_IMPROVISED_ITEM"] = "절박한 상황에는 극단적인 수단이 필요합니다. 오컬트 작전에 마땅한 물건이 없다면 페널티를 감수하고 즉흥적인 재료를 사용하세요.";
            _locKeyMap["ADVICE_ONE_CHANGED_ON_TRAVEL"] = "다른 도시로 이동하면 이 묶음의 아이템 1개가 다른 물건으로 변합니다.";
            _locKeyMap["ADVICE_ONE_CONSUMED_ON_TRAVEL"] = "다른 도시로 이동하면 이 묶음의 아이템 1개가 사라집니다.";
            _locKeyMap["ASPECTS_DISLIKED"] = "싫어함: ";
            _locKeyMap["ASPECTS_LIKED"] = "좋아함: ";
            _locKeyMap["COMMA"] = ", ";
            _locKeyMap["DIALOGUE_END_MARKER"] = "[끝] ";
            _locKeyMap["GAINED_ITEM"] = "{0} 획득 ({1})";
            _locKeyMap["LABEL_OPINION"] = "평판 - ";
            _locKeyMap["LOST_ITEM"] = "{0} 잃음 ({1})";
            _locKeyMap["NPC_RELATIONSHIP_WITH_PLAYER"] = "{0}: {1}";
            _locKeyMap["OPT_FULLSCREEN_WRONGRATIO_WARNING"] = "이 게임은 16:9 해상도에 최적화되어 있으나, 와이드스크린에서는 레터박스가 적용됩니다.";
            _locKeyMap["QCHANGE_CIRCUMSTANCE"] = "<b>상황이 변경되었습니다</b> {0}: {1}";
            _locKeyMap["QCHANGE_PLAIN"] = "{0} - {1}";
            _locKeyMap["QCHANGE_PLAN"] = "<b>일지 - {0}</b> - {1}";
            _locKeyMap["QCHANGE_THREAT"] = "<b>위협이 발생했습니다</b> - {0}: {1}";
            _locKeyMap["QCHANGE_UNSPECIFIED"] = "?? -{0} - {1}";
            _locKeyMap["QV_NOQLD_CHANGE"] = "{0}: 이전 {1} → 현재 {2}";
            _locKeyMap["RELATIONSHIP_IMPACT_CONSEQUENTIAL"] = "중대한";
            _locKeyMap["RELATIONSHIP_IMPACT_GRAND"] = "웅대한";
            _locKeyMap["RELATIONSHIP_IMPACT_MAJOR"] = "주요";
            _locKeyMap["RELATIONSHIP_IMPACT_MINOR"] = "사소한";
            _locKeyMap["RELATIONSHIP_IMPACT_SIGNIFICANT"] = "상당한";
            _locKeyMap["RELATIONSHIP_STATUS_APPRECIATIVE"] = "호의적임";
            _locKeyMap["RELATIONSHIP_STATUS_AVOWEDLYVENGEFUL"] = "공공연히 복수심을 품음";
            _locKeyMap["RELATIONSHIP_STATUS_FRIENDLY"] = "우호적임";
            _locKeyMap["RELATIONSHIP_STATUS_HOSTILE"] = "적대적임";
            _locKeyMap["RELATIONSHIP_STATUS_LOYAL"] = "충성스러움";
            _locKeyMap["RELATIONSHIP_STATUS_RECEPTIVE"] = "수용적임";
            _locKeyMap["RELATIONSHIP_STATUS_SUPREMELYLOYAL"] = "극도로 충성스러움";
            _locKeyMap["RELATIONSHIP_STATUS_SUSPICIOUS"] = "의심스러워함";
            _locKeyMap["RELATIONSHIP_STATUS_UNCERTAIN"] = "불확실함";
            _locKeyMap["RELATIONSHIP_STATUS_UNMET"] = "만나지 않음";
            _locKeyMap["RELATIONSHIP_STATUS_VENGEFUL"] = "복수심을 품음";
            _locKeyMap["RELATIONSHIP_STATUS_VERYAPPRECIATIVE"] = "매우 감사해함";
            _locKeyMap["RELATIONSHIP_STATUS_VERYFRIENDLY"] = "매우 우호적임";
            _locKeyMap["RELATIONSHIP_STATUS_VERYHOSTILE"] = "매우 적대적임";
            _locKeyMap["RELATIONSHIP_STATUS_VERYLOYAL"] = "매우 충성스러움";
            _locKeyMap["RELATIONSHIP_STATUS_VERYRECEPTIVE"] = "매우 수용적임";
            _locKeyMap["RELATIONSHIP_STATUS_VERYSUSPICIOUS"] = "매우 의심스러워함";
            _locKeyMap["RELATIONSHIP_STATUS_VERYVENGEFUL"] = "매우 복수심을 품음";
            _locKeyMap["RELATIONSHIP_STATUS_VERYWARY"] = "매우 경계함";
            _locKeyMap["RELATIONSHIP_STATUS_WARY"] = "경계함";
            _locKeyMap["REL_BETTER_INCREMENT"] = "{0}(이)가 찬성함 (+{1})";
            _locKeyMap["REL_BETTER_STEP"] = "{0}(이)가 찬성함 (+{1}), 현재 상태: {2}";
            _locKeyMap["REL_UNCHANGED"] = "{0}: 평판에 변화 없음";
            _locKeyMap["REL_WORSE_INCREMENT"] = "{0}(이)가 반대함 (-{1})";
            _locKeyMap["REL_WORSE_STEP"] = "{0}(이)가 반대함 (-{1}), 현재 상태: {2}";
            _locKeyMap["SKILL_CHECK_FAILURE_TROUBLE_WARNING"] = "{0} 판정에 실패하면 {1}(이)가 추가됩니다.";
            _locKeyMap["UI_ACTED_WITH_PASSION"] = "열정으로 행동함: {0}";
            _locKeyMap["UI_ADOPT_PASSION_SUMMARY"] = "{0} 채택";
            _locKeyMap["UI_ADOPT_PASSION_SUMMARY_WITH_ASPECT"] = "{0} 채택 ({1})";
            _locKeyMap["UI_AUTOSAVING"] = "자동 저장 중...";
            _locKeyMap["UI_BUG_REPORT_CRASH_TITLE"] = "이런 빌어먹을 - 충돌이 발생했습니다! 버그 리포트를 통해 문제를 해결할 수 있도록 도와주세요 - ";
            _locKeyMap["UI_BUG_REPORT_FAILURE_NETWORK"] = "게임이 인터넷에 연결할 수 없거나 서버가 다운되었습니다.";
            _locKeyMap["UI_BUG_REPORT_FAILURE_NOT_CONFIGURED"] = "이 빌드에는 버그 리포트 기능이 구성되지 않았습니다! support@weatherfactory.biz 로 버전 번호를 알려주세요.";
            _locKeyMap["UI_BUG_REPORT_FAILURE_RATE_LIMITED"] = "버그 리포트 전송 횟수가 제한되어 있습니다. 잠시 기다리시거나 support@weatherfactory.biz 로 이메일을 보내주세요.";
            _locKeyMap["UI_BUG_REPORT_SUCCESS_TITLE"] = "성공적으로 업로드되었습니다!";
            _locKeyMap["UI_BUG_REPORT_VALIDATION_DESCRIPTION"] = "설명 누락";
            _locKeyMap["UI_BUG_REPORT_VALIDATION_EMAIL"] = "이메일 누락";
            _locKeyMap["UI_CHARGEN_ASPECT_POOL_PASSIONS"] = "스펜서의 열정은 속성을 제공하여, 해당 속성을 사용하는 기술의 성공 확률을 높여줍니다. ";
            _locKeyMap["UI_CHARGEN_ASPECT_POOL_SIGN"] = "스펜서의 열정은 속성을 제공하여, 해당 속성을 사용하는 기술의 성공 확률을 높여줍니다. 최종 기술 선택에 따라 작은 추가 보너스를 제공하는 표식도 결정됩니다. 이 최종 속성 풀과 일치하는 현재 기술: <b>{0}</b>.";
            _locKeyMap["UI_CHARGEN_ASPECT_POOL_SKILLS"] = "스펜서의 열정은 속성을 제공하여, 해당 속성을 사용하는 기술의 성공 확률을 높여줍니다. 현재 적용 기술: <b>{0}</b>.";
            _locKeyMap["UI_COLLECT"] = "수령";
            _locKeyMap["UI_COMPANION_BONUSES"] = "{0}(이)가 도움 제공 - {1}";
            _locKeyMap["UI_CONDITION_AGAIN"] = "{0}, 다시 한 번.";
            _locKeyMap["UI_CONDITION_EXIT_AGAIN"] = "더 이상 {0} 상태 아님, 다시 한 번.";
            _locKeyMap["UI_CONFIRM"] = "확인";
            _locKeyMap["UI_CRAFT"] = "제작";
            _locKeyMap["UI_CRAFTING_FILL_SLOTS"] = "모든 슬롯 채우기";
            _locKeyMap["UI_CRAFTING_PLEASE_SELECT_DESC"] = "어디서든 레시피를 확인하세요. 장소와 레시피가 기술과 일치하는 곳에서 제작할 수 있습니다.";
            _locKeyMap["UI_CRAFTING_PLEASE_SELECT_TITLE"] = "레시피를 선택하세요...";
            _locKeyMap["UI_CRAFTING_SKILLS_OR"] = ", 또는 ";
            _locKeyMap["UI_CRAFTING_SKILL_AT_VENUES_ENTRY"] = "{0} ({1} 내)";
            _locKeyMap["UI_CRAFTING_SKILL_LOCKED"] = "이 레시피를 제작하려면 기술을 향상시키세요";
            _locKeyMap["UI_CRAFTING_VENUE_NEEDS_SKILL"] = "레시피가 장소에 부합하지만, 장소에 기술 {0}(이)가 필요합니다";
            _locKeyMap["UI_CRAFTING_VENUE_OR"] = " 또는 ";
            _locKeyMap["UI_CRAFTING_VENUE_SKILL_INSUFFICIENT"] = "이 레시피를 제작하기에 충분한 기술을 갖추었으나… {0}(이)가 있어야만 가능합니다";
            _locKeyMap["UI_CRAFTING_WRONG_VENUE"] = "제작 장소: {0}";
            _locKeyMap["UI_CURRENCY_SHORT_cashBritain"] = "£";
            _locKeyMap["UI_CURRENCY_SHORT_cashEastGermany"] = "OM";
            _locKeyMap["UI_CURRENCY_SHORT_cashRepublic"] = "FR";
            _locKeyMap["UI_CURRENCY_SHORT_cashSoviet"] = "*";
            _locKeyMap["UI_CURRENCY_SHORT_cashState"] = "FQ";
            _locKeyMap["UI_CURRENCY_SHORT_cashSwiss"] = "CHF";
            _locKeyMap["UI_CURRENCY_SHORT_cashUSA"] = "$";
            _locKeyMap["UI_CURRENCY_SHORT_cashWestGermany"] = "DM";
            _locKeyMap["UI_CURRENCY_SHORT_cigarette"] = "CIG";
            _locKeyMap["UI_DATA_COLLECTION_ACCEPTED"] = "예";
            _locKeyMap["UI_DATA_COLLECTION_ACCEPTED_HINT"] = "스토리 및 설정 선택에 관한 가명화된 데이터를 수집합니다";
            _locKeyMap["UI_DATA_COLLECTION_ACCEPTED_PLUS"] = "연동";
            _locKeyMap["UI_DATA_COLLECTION_ACCEPTED_PLUS_HINT"] = "예, 버그 리포트와의 연동에도 동의합니다";
            _locKeyMap["UI_DATA_COLLECTION_DENIED"] = "아니오";
            _locKeyMap["UI_DATA_COLLECTION_DENIED_HINT"] = "스토리 및 설정 선택에 관한 어떤 데이터도 수집하지 않습니다";
            _locKeyMap["UI_DEFAULT_CONVERSATION_TITLE"] = "말하기, 듣기";
            _locKeyMap["UI_DESCRIPTION_CONCEALED"] = "???";
            _locKeyMap["UI_DIALOGUESPEED_QUICK"] = "빠르게";
            _locKeyMap["UI_DIALOGUESPEED_QUICKEST"] = "선명하게";
            _locKeyMap["UI_DIALOGUESPEED_SLOW"] = "차분하게";
            _locKeyMap["UI_DIALOGUETEXTSIZE_LARGE"] = "크게";
            _locKeyMap["UI_DIALOGUETEXTSIZE_LARGEST"] = "가장 크게";
            _locKeyMap["UI_DIALOGUETEXTSIZE_STANDARD"] = "작게";
            _locKeyMap["UI_DOFFED_OUTFIT"] = "의상 벗음: {0}";
            _locKeyMap["UI_DONNED_OUTFIT"] = "의상 착용: {0}";
            _locKeyMap["UI_ELLIPSIS"] = "...";
            _locKeyMap["UI_ESCAPE_CONFIRM_KNOCK"] = "정말 탈출! 명령을 사용해 스펜서를 꺼내시겠습니까? 제대로 끼지 않았다면 사용하지 마세요! 스펜서에게 좋지 않습니다!";
            _locKeyMap["UI_ESCAPE_CONFIRM_MAIN_MENU"] = "정말 메인 메뉴로 나가시겠습니까?";
            _locKeyMap["UI_ESCAPE_CONFIRM_QUIT"] = "정말 바탕화면으로 나가시겠습니까?";
            _locKeyMap["UI_ESCAPE_KNOCK_TAB_BUTTON"] = "끼임 해결";
            _locKeyMap["UI_ESCAPE_KNOCK_TAB_DESC"] = "길찾기 버그로 스펜서가 끼었을 때 탈출시켜 줍니다. 제대로 끼지 않았다면 사용하지 마세요! 스펜서에게 좋지 않습니다!";
            _locKeyMap["UI_ESCAPE_KNOCK_TAB_LABEL"] = "탈출!";
            _locKeyMap["UI_EVOLVED_PASSION"] = "열정 획득! ({0})";
            _locKeyMap["UI_EVOLVE_TO"] = "{0}(으)로 진화하시겠습니까?";
            _locKeyMap["UI_EXP"] = "EXP";
            _locKeyMap["UI_EXPERIENCE_GAIN"] = "{0} {1} 경험 획득";
            _locKeyMap["UI_EXPERIENCE_GAIN_ONE"] = "{0} 경험 1 획득";
            _locKeyMap["UI_EXPERIENCE_SPENT"] = "{0} {1} 경험 소모";
            _locKeyMap["UI_EXPERIENCE_SPENT_MIXED"] = "경험 {0} 소모";
            _locKeyMap["UI_EXP_COST"] = "(경험 {0})";
            _locKeyMap["UI_FAILURE"] = "{0} - 실패";
            _locKeyMap["UI_FATE"] = "운명";
            _locKeyMap["UI_FOOTNOTE_SUBTLE"] = "은밀함";
            _locKeyMap["UI_FOOTNOTE_SUBTLE_HINT"] = "최초 1회만";
            _locKeyMap["UI_FOOTNOTE_UNSUBTLE"] = "성가심";
            _locKeyMap["UI_FOOTNOTE_UNSUBTLE_HINT"] = "매회";
            _locKeyMap["UI_FORCED_SUCCESS"] = "{0} - 강제 성공";
            _locKeyMap["UI_GRAPHICS_HIGH"] = "높음";
            _locKeyMap["UI_GRAPHICS_LOW"] = "낮음";
            _locKeyMap["UI_GRAPHICS_MED"] = "보통";
            _locKeyMap["UI_HUD_HOTKEY_CHARACTER"] = "C";
            _locKeyMap["UI_HUD_HOTKEY_FOOTNOTES"] = "F";
            _locKeyMap["UI_HUD_HOTKEY_INVENTORY"] = "I";
            _locKeyMap["UI_HUD_HOTKEY_JOURNAL"] = "J";
            _locKeyMap["UI_HUD_HOTKEY_MAP"] = "M";
            _locKeyMap["UI_HUD_HOTKEY_OPTIONS"] = "Esc";
            _locKeyMap["UI_HUD_HOTKEY_RECIPES"] = "R";
            _locKeyMap["UI_HUD_HOVER_CHARACTER"] = "인물";
            _locKeyMap["UI_HUD_HOVER_FOOTNOTES"] = "각주";
            _locKeyMap["UI_HUD_HOVER_INVENTORY"] = "소지품";
            _locKeyMap["UI_HUD_HOVER_JOURNAL"] = "일지";
            _locKeyMap["UI_HUD_HOVER_MAP"] = "지도";
            _locKeyMap["UI_HUD_HOVER_OPTIONS"] = "옵션";
            _locKeyMap["UI_HUD_HOVER_RECIPES"] = "레시피";
            _locKeyMap["UI_IMPROVE_WITH"] = "{0} {1} 경험으로 향상";
            _locKeyMap["UI_IMPROVISED_WITH"] = "{0}에 즉흥적인 재료를 사용함 ";
            _locKeyMap["UI_INCREASE_MIXED"] = "증가...";
            _locKeyMap["UI_INCREASE_WITH"] = "{0}(으)로 증가";
            _locKeyMap["UI_INCREASE_WITH_MIXED_HEADER"] = "다음을 사용하여 증가...";
            _locKeyMap["UI_ITEM_CATEGORY_ALL"] = "전체";
            _locKeyMap["UI_ITEM_CATEGORY_Papers"] = "서류";
            _locKeyMap["UI_ITEM_CATEGORY_Resources"] = "자원";
            _locKeyMap["UI_ITEM_CATEGORY_Tools"] = "도구";
            _locKeyMap["UI_ITEM_CATEGORY_Troubles"] = "고난";
            _locKeyMap["UI_ITEM_WAS_EXHAUSTED"] = "사용 시 소모됨: {0}";
            _locKeyMap["UI_LOAD_FAILED"] = "불러올 수 없습니다!";
            _locKeyMap["UI_LOAD_INCOMPATIBLE"] = "죄송합니다 - 이 저장 파일은 현재 게임 버전에서 사용하기에 너무 오래되었습니다.";
            _locKeyMap["UI_LOCKED_SLOT"] = "잠긴 슬롯";
            _locKeyMap["UI_LOCKED_TRAVEL_REQUIREMENT_DESCRIPTION"] = "<i>자세한 내용을 확인하려면 이것들 중 하나를 찾으세요.</i>";
            _locKeyMap["UI_LOST_OUTFIT"] = "의상 잃음: {0}";
            _locKeyMap["UI_MERCHANT_AGIO_FORMAT"] = "{0}{1} 액면가 · {0}{2} 환전 수수료 (<i>할증료</i>)";
            _locKeyMap["UI_MERCHANT_AMOUNT_TO_BUY"] = "구매 수량";
            _locKeyMap["UI_MERCHANT_AMOUNT_TO_SELL"] = "판매 수량";
            _locKeyMap["UI_MERCHANT_BADGE_FOREIGN"] = "{0}%";
            _locKeyMap["UI_MERCHANT_BADGE_NATIVE"] = "1:1";
            _locKeyMap["UI_MERCHANT_BUYS"] = "{0} 구매";
            _locKeyMap["UI_MERCHANT_CONFIRM_OVERPAY"] = "대금을 과도하게 지불하고 있습니다! {0}{1}을(를) {0}{2}의 대가로 지불합니다. 진행하시겠습니까?";
            _locKeyMap["UI_MERCHANT_SELLS_PAID_IN"] = "이 상인은 {0}(으)로 지불합니다";
            _locKeyMap["UI_MERCHANT_TOTAL_FORMAT"] = "합계: {1} {0}{2} {3}";
            _locKeyMap["UI_MERCHANT_WARN_CANT_PAY"] = "지불 불가: {1} {0}{2} {3}";
            _locKeyMap["UI_MERCHANT_WARN_MERCHANT_CANT_COVER"] = "상인이 이 대금을 감당할 수 없습니다!";
            _locKeyMap["UI_MERCHANT_WARN_TRIVIAL"] = "거래액이 너무 사소합니다!";
            _locKeyMap["UI_NONE"] = "없음";
            _locKeyMap["UI_NOT"] = "{0} 아님";
            _locKeyMap["UI_NO_COMPANION_BONUS"] = "{0}(은)는 기술에 아무런 도움이 되지 않습니다.";
            _locKeyMap["UI_OPTIONS_TAB_ADJUSTMENTS"] = "<size=80%><line-height=90%>\"'그걸 산산조각 낸 건 우리 일행이었소.' 기르비가 불쑥 말하더니, 다시 쇳물 같은 눈물을 흘린다...\" — 수사슴의 문에서의 대화</line-height></size>";
            _locKeyMap["UI_OPTIONS_TAB_AUDIO"] = "<size=80%><line-height=90%>\"모든 벼락 속에는 공통된 정서가 깃들어 있다. 이를 인정하도록 하자.\" — 우레와 같은 비밀</line-height></size>";
            _locKeyMap["UI_OPTIONS_TAB_CONTROLS"] = "<size=80%><line-height=90%>\"'모든 열쇠엔 자물쇠가 있다'는 말은 '모든 스패너엔 너트가 있다'보다 훨씬 거창하게 들린다. 하지만 둘 다 사실이다.\" — 세레나 블랙우드</line-height></size>";
            _locKeyMap["UI_OPTIONS_TAB_VISUALS"] = "<size=80%><line-height=90%>\"시선의 도둑, 왕좌의 청소부. 국경의 약탈자이자 매듭의 도둑. 색채의 도둑이자 뼈의 도둑.\" — 조형(鳥形)의 시간들의 장난</line-height></size>";
            _locKeyMap["UI_PERCENTAGE_CHANCE"] = "{0}%";
            _locKeyMap["UI_PICK_SOMETHING_OUT"] = "물건 고르기";
            _locKeyMap["UI_PLAN_CHOOSE_REWARD"] = "보상을 선택하세요:";
            _locKeyMap["UI_PLAN_COMPLETED"] = "{0} 완료! 일지 페이지에서 경험을 모으세요.";
            _locKeyMap["UI_PLAN_REWARD_CHOSEN"] = "선택된 보상:";
            _locKeyMap["UI_PLAN_REWARD_GAINED"] = "+{0} {1} 경험 획득";
            _locKeyMap["UI_PLAN_REWARD_GAIN_OPTION"] = "{0} 경험 획득";
            _locKeyMap["UI_PROVIDES_ASPECTS_POOL"] = "성공 확률을 강제하는 데 도움이 되도록 풀에 이 속성들을 제공합니다.";
            _locKeyMap["UI_QUICKSAVING"] = "빠른 저장 중...";
            _locKeyMap["UI_RECIPE_BOOK_HEADER"] = "알려진 레시피";
            _locKeyMap["UI_RECIPE_SLOT_REQS_CATALYST"] = "속성 {0}(을)를 지닌 재료를 추가하세요. 레시피에서 소모되지 않습니다.";
            _locKeyMap["UI_RECIPE_SLOT_REQS_FUEL"] = "속성 {0}(을)를 지닌 재료를 추가하세요. 레시피에서 소모됩니다.";
            _locKeyMap["UI_RECOVERED_POOL_ASPECTS"] = "속성 풀이 {0}만큼 회복되었습니다";
            _locKeyMap["UI_REJECTED_PASSION"] = "거부된 열정: {0}";
            _locKeyMap["UI_REJECT_PASSION_SUMMARY"] = "{0} 거부";
            _locKeyMap["UI_RESULT_FAILURE"] = "실패";
            _locKeyMap["UI_RESULT_SUCCESS"] = "성공";
            _locKeyMap["UI_SAVE_CATEGORY_AUTOSAVE"] = "자동 저장";
            _locKeyMap["UI_SAVE_CATEGORY_FINAL"] = "게임 완료";
            _locKeyMap["UI_SAVE_CATEGORY_FINAL_DISALLOWED_MESSAGE"] = "데모 버전에서는 엔딩 이후 불러오기가 불가능합니다 :(";
            _locKeyMap["UI_SAVE_CATEGORY_QUICKSAVE"] = "빠른 저장";
            _locKeyMap["UI_SAVE_CONFIRM_DELETE"] = "이 저장 파일이 영구적으로 삭제되며, 되돌릴 수 없습니다!";
            _locKeyMap["UI_SAVE_CONFIRM_DELETE_BUTTON"] = "저장 파일 삭제";
            _locKeyMap["UI_SAVE_CONFIRM_DELETE_TITLE"] = "정말 저장 파일을 삭제하시겠습니까?";
            _locKeyMap["UI_SAVE_CONFIRM_LOAD"] = "저장되지 않은 현재 진행 상황을 잃게 됩니다.";
            _locKeyMap["UI_SAVE_CONFIRM_LOAD_BUTTON"] = "저장 파일 불러오기";
            _locKeyMap["UI_SAVE_CONFIRM_LOAD_TITLE"] = "선택한 저장 파일을 불러오시겠습니까?";
            _locKeyMap["UI_SAVE_CONFIRM_OVERWRITE"] = "이 저장 파일 위에 영구적으로 덮어쓰며, 되돌릴 수 없습니다!";
            _locKeyMap["UI_SAVE_CONFIRM_OVERWRITE_BUTTON"] = "확인 및 저장";
            _locKeyMap["UI_SAVE_CONFIRM_OVERWRITE_TITLE"] = "정말 저장 파일을 덮어쓰시겠습니까?";
            _locKeyMap["UI_SAVE_FAILED"] = "저장할 수 없습니다 - {0}의 접근 권한을 확인하세요";
            _locKeyMap["UI_SAVE_INCOMPATIBLE"] = "호환되지 않는 구버전 저장 파일";
            _locKeyMap["UI_SAVE_SLOTS_FULL"] = "최대 저장 파일 수에 도달했습니다! (축하드립니다?) 기존 저장을 삭제하거나 덮어쓰세요.";
            _locKeyMap["UI_SAVING"] = "저장 중...";
            _locKeyMap["UI_SCREENMODE_EXCLUSIVEFULLSCREEN"] = "전체 화면";
            _locKeyMap["UI_SCREENMODE_FULLSCREENWINDOW"] = "테두리 없는 창";
            _locKeyMap["UI_SCREENMODE_MAXIMIZEDWINDOW"] = "최대화된 창 화면";
            _locKeyMap["UI_SCREENMODE_WINDOWED"] = "창 모드";
            _locKeyMap["UI_PERFORMANCE_CURRENT_SIGN"] = "현재 당신의 표식: 공연 중 다음에 영향을 미칩니다";
            _locKeyMap["UI_SEMICOLON"] = ";";
            _locKeyMap["UI_SKILLCHECKRANDOMNESS_PURERANDOM"] = "완전 무작위 (연속 성공/실패 가능)";
            _locKeyMap["UI_SKILLCHECKRANDOMNESS_SHUFFLEBAGPERSISTED"] = "균형 모드 (불러오기 시 리셋 없음)";
            _locKeyMap["UI_SKILLCHECKRANDOMNESS_SHUFFLEBAGVOLATILE"] = "균형 모드 (불러오기 시 리셋)";
            _locKeyMap["UI_SKILL_CHECK_ACCEPTED_FAILURE"] = "실패!";
            _locKeyMap["UI_SKILL_CHECK_FAILURE"] = "실패?";
            _locKeyMap["UI_SKILL_CHECK_FORCED_SUCCESS"] = "기사회생 성공!";
            _locKeyMap["UI_SKILL_CHECK_HEADER"] = "기술 판정: {0}";
            _locKeyMap["UI_SKILL_CHECK_MOD_DESC"] = "{0} 보너스를 {1} 기술 판정에 부여";
            _locKeyMap["UI_SKILL_CHECK_MOD_DESC_ALL"] = "모든 기술 판정에 {0}";
            _locKeyMap["UI_SKILL_CHECK_SUCCESS"] = "성공";
            _locKeyMap["UI_SOLD_OUT"] = "품절";
            _locKeyMap["UI_SUCCESS"] = "{0} - 성공";
            _locKeyMap["UI_TRAVEL_ACROSS_BORDER"] = "스펜서는 이곳으로 이동할 수 있습니다. 국경을 넘는 데 필요한 서류를 갖추었습니다.";
            _locKeyMap["UI_TRAVEL_ALREADY_THERE"] = "스펜서의 현재 위치";
            _locKeyMap["UI_TRAVEL_CANNOT_CROSS_BORDER"] = "스펜서는 적절한 서류를 갖출 때까지 국경을 넘을 수 없습니다.";
            _locKeyMap["UI_TRAVEL_CAN_TRAVEL_HERE"] = "스펜서는 이곳으로 이동할 수 있습니다.";
            _locKeyMap["UI_TRAVEL_DESTINATION_INVALID"] = "문제가 발생했습니다: 목적지를 이용할 수 없습니다";
            _locKeyMap["UI_TRAVEL_INSIDE_BORDERS"] = "스펜서는 국경을 넘지 않고 이곳으로 이동할 수 있습니다.";
            _locKeyMap["UI_TRAVEL_NO_TRACES"] = "이 경로는 스펜서에게 <sprite=\"QualityIcons\" name=\"trace\">가 있다면 이를 일부 정화해 줍니다.";
            _locKeyMap["UI_TRAVEL_RESTRICTED_FOR_NOW"] = "아직입니다! 먼저 {0}(으)로 이동하세요.";
            _locKeyMap["UI_TRAVEL_TOO_FAR"] = "스펜서는 인접한 역에서 이곳으로 이동할 수 있습니다.";
            _locKeyMap["UI_TRAVEL_TRACES_GAIN"] = "너무 이른 시간에 다시 이곳을 지나면 {0}개의 <sprite=\"QualityIcons\" name=\"trace\">가 쌓입니다.";
            _locKeyMap["UI_TRAVEL_TRACES_LOSE"] = "이 경로는 {0}개의 <sprite=\"QualityIcons\" name=\"trace\">를 제거합니다";
            _locKeyMap["UI_TRAVEL_UNAVAILABLE_BY_RAIL"] = "서커스단 기차는 이곳으로 이동할 수 없거나 이동하지 않을 것입니다.";
            _locKeyMap["UI_UNDRESS"] = "옷 벗기";
            _locKeyMap["UI_UNLOCKED_OUTFIT"] = "해금된 의상: {0}";
            _locKeyMap["UI_UNLOCK_MIXED"] = "해금...";
            _locKeyMap["UI_UNLOCK_WITH"] = "{0}(으)로 해금";
            _locKeyMap["UI_UNSHROUD_TO_LEARN_MORE"] = "자세한 내용을 확인하려면 이 레시피의 베일을 벗기세요...";
            _locKeyMap["UI_USES_ASPECTS_POOL"] = "성공을 강제하고자 할 때 풀의 이 속성들에 의존합니다.";
            _locKeyMap["UI_VS"] = "대";
            _locKeyMap["UNCHANGED_ITEM_COUNT"] = "{0}: {1}";
            _locKeyMap["VGROUP_ANTIBES"] = "앙티브";
            _locKeyMap["VGROUP_CAEN"] = "캉";
            _locKeyMap["VGROUP_CHAIMA"] = "샤이마";
            _locKeyMap["VGROUP_CORSO"] = "코르소";
            _locKeyMap["VGROUP_DAFFER"] = "대퍼";
            _locKeyMap["VGROUP_KERISHAM"] = "케리셤";
            _locKeyMap["VGROUP_MIMATA"] = "미마타";
            _locKeyMap["VGROUP_ROSAMUNDI"] = "로사 문디";
            _locKeyMap["VGROUP_SANITARIUM"] = "요양원";
            _locKeyMap["VGROUP_SUNDRY"] = "기타 잡화";
            _locKeyMap["VGROUP_VINCENT"] = "뱅상";
        }
    }
}
