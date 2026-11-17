using System;
using System.IO;
using Travelling.Korean;

class VerifyV10
{
    static void Main()
    {
        KoreanPatch.Initialize();

        string[][] tests = new string[][] {
            // Category 1: Double-bracket spaces [[Tag] ] and [[[Tag] ]]
            new[] { "Cat1_DoubleBracketSpace1", "[[Passion] ]과 [[Nina Lagasse] ]라는", "<link=\"Passion\">열정</link></color>과" },
            new[] { "Cat1_TripleBracketSpace", "[[[Nina Lagasse] ]를 찾아", "<link=\"Nina Lagasse\">니나 라가스</link></color>를 찾아" },

            // Category 2: Missing space after [[Link]] before standalone words
            new[] { "Cat2_LinkSpace1", "[[Hushery]]모두", "</color> 모두" },
            new[] { "Cat2_LinkSpace2", "[[Amiral]]만세!", "<link=\"Amiral\">다르랑 제독</link></color> 만세!" },
            new[] { "Cat2_LinkSpace3", "[[Skills]]및 열정", "</color> 및 열정" },

            // Category 3: Inline Latin/French/English words removed + particles fixed
            new[] { "Cat3_Owl", "[Potential] But Picasso - Picasso had an <i><u>owl</u></i>.", "<i><u>부엉이</u></i>가" },
            new[] { "Cat3_Ubu", "He called the owl 'Ubu'.", "'위뷔'" },
            new[] { "Cat3_Voila", "<i>Voilà.</i>", "<i>자, 여기요.</i>" },
            new[] { "Cat3_Poinconneuse", "The <i>poinçonneuse</i> waits at the barrier, moodily clicking her ticket-clipper. Beyond the barrier, at the platform, the train gleams and growls.", "<i>개찰원</i>이" },
            new[] { "Cat3_Cambronne", "<i>Cambronne!</i> The cat from the Sanitarium? That thing really does get everywhere. I think it's possessed.", "<i>젠장!</i>" },
            new[] { "Cat3_SmokeGhost", "[Run fingers over the autobiography of a stage magician: SMOKE GHOST.]", "《연기 유령》" },
            new[] { "Cat3_Zenith", "Lambent brass, and black bakelite shiny as a beetle's carapace. A handsome bit of kit. And remarkably portable - less than two feet across. It's branded with a crown, and the words ZENITH TRANS-OCEANIC.", "제니스 트랜스오셔닉" },

            // Category 4: Dialogue lines with [q=...] and missing lines
            new[] { "Cat4_Bill8", "That comes to [q=chezfelix.bill:words] francisques.", "총 [q=chezfelix.bill:words]프랑시스크입니다." },
            new[] { "Cat4_Camomile", "Camomile ?", "카모마일로 하시겠어요?" },
            new[] { "Cat4_CocaCola", "Coca-Cola ?", "코카콜라로 하시겠어요?" },
            new[] { "Cat4_Nihilist", "<size=60%>(Nihilist.)</size>", "<size=60%>(허무주의자 같으니.)</size>" },
            new[] { "Cat4_Spence", "Spence !", "스펜스!" },
            new[] { "Cat4_WelcomeBack", "[q=alias.formal.fr]. Welcome back. How may I assist you today?", "다시 뵙는군요" },
            new[] { "Cat4_Apologise", "I apologise, [q=alias.formal], for these savages. If they had better manners, they'd introduce themselves.", "야만인들을 대신해 사과드립니다" },

            // Category 5: Yellow Conversation Title Banners (128 descriptions)
            new[] { "Cat5_Banner1", "'POSTERS FOR POETRY, NEWSPAPERS FOR PROSE'", "'시는 포스터에, 산문은 신문에'" },
            new[] { "Cat5_Banner2", "CHEZ FELIX, SANS FELIX", "펠릭스 없는 셰 펠릭스" },
            new[] { "Cat5_Banner3", "CHARMION VON WIEGAND, 'THE NUPTIAL FORM', 1947 [OIL, CANVAS]", "샤미언 본 위갠드, '혼례의 형상', 1947 [캔버스에 유채]" },
            new[] { "Cat5_Banner4", "'I WILL STRIVE, M. HOBSON; I WILL ACHIEVE; I WILL RISE'", "'저는 분투할 것입니다, 홉슨 씨. 성취할 것이고, 비상할 것입니다'" },

            // Category 6: Inline Alerts (✧) & ConditionQuality & Situation messages
            new[] { "Cat6_AlertChilly", "<i><sprite=\"inline\" name=\"star\"> A welcome warmth. I'm no longer Chilly.</i>", "반가운 온기다" },
            new[] { "Cat6_AlertUneasy", "<i><sprite=\"inline\" name=\"star\"> [q=Dread] Dread - I'm growing Uneasy.</i>", "불안해진다" },
            new[] { "Cat6_AlertTraces", "Traces [q=trace] - I fear I'm no longer just the subject of Rumour, but under active Surveillance.", "본격적인 감시를 받고 있는 듯하다" },
            new[] { "Cat6_AlertPursued", "My Traces have increased to [q=trace]. I'm being Pursued now.", "이제 추적당하고 있다" },
            new[] { "Cat6_AlertLanguor", "At [q=Weariness] Weariness, my Languor has gone.", "나른함마저 가셨다" },

            // Category 7: Memory items with commas
            new[] { "Cat7_MemoryOulio", "Memory: St Oulio's Eve, Cambray, 1919", "기억: 성 울리오 전야, 캉브레, 1919년" },

            // Category 8 & 9: Bracketed choice requirements & [<u>Success</u>]
            new[] { "Cat8_BracketQty", "[50 Francisques; Two Orders per Visit] I've brought no custom yet.", "[프랑시스크 50; 방문당 주문 2건]" },
            new[] { "Cat8_BracketCrit", "[Large Manila Envelope 1+] Read it", "[대형 마닐라 봉투 1+]" },
            new[] { "Cat8_BracketOpinion", "[Opinion - Linou - 대단히 호의적임] Ask her", "[리누에 대한 평판 - 대단히 호의적임]" },
            new[] { "Cat8_BracketCurly", "[Heart’s Desire?; The Secrets of Dr A.] Wait", "[마음의 갈망?; A 박사의 비밀]" },
            new[] { "Cat9_Success", "[<u>Success</u>] Done", "[<u>성공</u>]" },
            new[] { "Cat9_Failure", "[<u>Failure</u>] Oops", "[<u>실패</u>]" },

            // Category 10: Crafting slot & Aspect pool recovery
            new[] { "Cat10_SlotReqs", "<i>속성 Influence, Lantern(을)를 지닌 재료를 추가하세요. 레시피에서 소모되지 않습니다.</i>", "<i>속성 영향력, 등불을 지닌 재료를 추가하세요. 레시피에서 소모되지 않습니다.</i>" },
            new[] { "Cat10_PoolRecov", "속성 풀이 1 Knock, 1 Lantern만큼 회복되었습니다", "속성 풀이 두드림 1, 등불 1만큼 회복되었습니다" },

            // Category 11: Skill & Aspect Modifiers
            new[] { "Cat11_SkillMod1", "Observation +2", "관찰 +2" },
            new[] { "Cat11_SkillMod2", "<i>Eloquence +2</i>", "<i>달변 +2</i>" },
            new[] { "Cat11_SkillVs", "Eloquence +0 vs 10;Birdsong +0 vs 10", "달변 +0 대 10; 새들의 노래 +0 대 10" },
            new[] { "Cat11_SmolPicker", "<sprite=\"SmolSkillImages\" name=\"Birdsong\"><space=-1.5em><sprite=\"SmolSkillImages\" name=\"outline\" color=#D24B00FF> Birdsong <color=#808080>[10%] → </color> [10%]", "새들의 노래 <color=#808080>[10%] → </color> [10%]" },
            new[] { "Cat11_AspectLiked", "+3 좋아함: Raffish", "+3 좋아함: 자유분방함" },
            new[] { "Cat11_CompanionBonus", "뱅상(이)가 도움 제공 - Charm: +2; Preservation: +2", "뱅상이 도움 제공 - 매력: +2; 보존: +2" },

            // Category 12: Trouble Tooltips & [Worldly Skill]
            new[] { "Cat12_TroubleDread", "1-3 Dread: \"This may not end well.\"\n\nCurrent Dread: [q=dread]. Penalties at 4+...", "현재 공포: [q=dread]" },
            new[] { "Cat12_WorldlySkill", "[A [[Worldly Skill]].]", "세속 기술" },

            // Category 13: Journal Red Bar
            new[] { "Cat13_JournalRedBar", "<uppercase>Performed</uppercase> -- I've performed a gentleman's rites this morning.", "<uppercase>치장함</uppercase> -- 오늘 아침에는 신사의 몸단장을 마쳤다." },

            // Category 14: Shop Truncated Label & Sold Out
            new[] { "Cat14_ShopTrunc", "GRENADINE CLARET…", "그레나딘 클라레 손수건" },
            new[] { "Cat14_SoldOut", "<size=18>SOLD OUT</size>", "<size=18>품절</size>" },

            // Category 16: CONTINUE button & Settings quote size
            new[] { "Cat16_Continue", "<cspace=1.8px>CONTINUE</cspace>", "<cspace=1.8px>계속</cspace>" },
            new[] { "Cat16_Settings", KoreanPatch.TranslateLocKey("UI_OPTIONS_TAB_ADJUSTMENTS") ?? "", "<size=80%>" },

            // Category 17: 이(가) particle
            new[] { "Cat17_AltParticle", "젤리아 부인이(가) 반대하며 (-1)", "젤리아 부인이 반대하며 (-1)" },

            // Category 18: Tone & Lore fixes
            new[] { "Cat18_Zelia", "I am not. And there are no holes. And the matter is in any case irrelevant.", "내가 무슨! 그리고 구덩이 따윈 없다. 어차피 다 상관없는 일이야." },
            new[] { "Cat18_CardPlayer", "Watch out! Linou always overcharges foreigners!", "조심하쇼! 리누는 외국인들한테 늘 바가지를 씌우니까!" },
            new[] { "Cat18_AndreeGraveyard", "You've found my graveyard.", "제 묘지를 찾아내셨군요." },
            new[] { "Cat18_Sinombre", "Leave that alone, you ridiculous man. I'm much more exciting and ten times as bright.", "그건 내버려 두렴, 우스꽝스러운 인간 같으니." },
            new[] { "Cat18_WormInfest", "I gave up tobacco back in the thirties. My passengers hated smoke. Something you wouldn't expect from Worm infestation: better skin, better teeth.", "벌레의 기생에서" },
            new[] { "Cat18_JaumeSpeaker", KoreanPatch.TranslateSpeakerName("Father Jaume"), "조메 신부" },
            new[] { "Cat18_LeopoldSpeaker", KoreanPatch.TranslateSpeakerName("Leopold"), "레오폴드" }
        };

        int passed = 0;
        int failed = 0;
        foreach (var t in tests)
        {
            string name = t[0];
            string input = t[1];
            string expectedSub = t[2];
            string actual = name.EndsWith("Speaker") || name == "Cat16_Settings" ? input : KoreanPatch.Translate(input);
            if (actual != null && actual.Contains(expectedSub))
            {
                passed++;
            }
            else
            {
                failed++;
                Console.WriteLine("FAIL [" + name + "]:");
                Console.WriteLine("  IN : " + input);
                Console.WriteLine("  EXP: " + expectedSub);
                Console.WriteLine("  ACT: " + actual);
            }
        }

        // Test TranslateLocArgs with comma-containing Memory item and Omelette alert
        string tmpl1 = "회상했습니다! {0}";
        string[] args1 = new[] { "Memory: St Oulio's Eve, Cambray, 1919" };
        KoreanPatch.TranslateLocArgs(ref tmpl1, args1);
        if (args1[0] == "기억: 성 울리오 전야, 캉브레, 1919년") passed++;
        else { failed++; Console.WriteLine("FAIL [LocArgs_Memory]: " + args1[0]); }

        string tmpl2 = "<b>상황이 변경되었습니다</b> {0}: {1}";
        string[] args2 = new[] { "Bessy's Omelette", "Nothing remains of this omelette, but there'll be another in time." };
        KoreanPatch.TranslateLocArgs(ref tmpl2, args2);
        if (args2[0] == "베시의 오믈렛" && args2[1].Contains("오믈렛은 흔적도 없이 사라졌지만")) passed++;
        else { failed++; Console.WriteLine("FAIL [LocArgs_Omelette]: " + args2[0] + " | " + args2[1]); }

        Console.WriteLine(string.Format("VERIFICATION SUMMARY: {0} PASSED, {1} FAILED", passed, failed));
        if (failed > 0) Environment.Exit(1);
    }
}
