using System;
using System.IO;
using Travelling.Korean;

class VerifyV12
{
    static int passed = 0;
    static int failed = 0;

    static void Assert(string name, bool condition, string detail = "")
    {
        if (condition)
        {
            Console.WriteLine("[PASS] " + name);
            passed++;
        }
        else
        {
            Console.WriteLine("[FAIL] " + name + (string.IsNullOrEmpty(detail) ? "" : " -> " + detail));
            failed++;
        }
    }

    static void Main()
    {
        KoreanPatch.Initialize();

        // 1. V11 regression tests
        string trKnock = KoreanPatch.Translate("Knock");
        Assert("Knock in Translate", trKnock == "두드림", "Got: " + trKnock);

        string trInvBag = KoreanPatch.Translate("Invisible Bag");
        Assert("Invisible Bag in Translate", trInvBag == "보이지 않는 가방", "Got: " + trInvBag);

        string trWhy = KoreanPatch.Translate("Why?");
        Assert("Why? dialogue tone", trWhy == "어째서입니까?", "Got: " + trWhy);

        string trUses = KoreanPatch.Translate("Uses");
        Assert("Uses shortened to 횟수", trUses == "횟수", "Got: " + trUses);

        // 2. New Loc Keys (v2026.11)
        string locSign = KoreanPatch.TranslateLocKey("UI_PERFORMANCE_CURRENT_SIGN");
        Assert("UI_PERFORMANCE_CURRENT_SIGN", locSign == "현재 당신의 표식: 공연 중 다음에 영향을 미칩니다", "Got: " + locSign);

        string locMaxWin = KoreanPatch.TranslateLocKey("UI_SCREENMODE_MAXIMIZEDWINDOW");
        Assert("UI_SCREENMODE_MAXIMIZEDWINDOW", locMaxWin == "최대화된 창 화면", "Got: " + locMaxWin);

        // 3. New Speakers
        string spAstorre = KoreanPatch.TranslateSpeakerName("Astorre");
        Assert("Speaker Astorre", spAstorre == "아스토레", "Got: " + spAstorre);

        string spBebette = KoreanPatch.TranslateSpeakerName("Bébette");
        Assert("Speaker Bébette", spBebette == "베베트", "Got: " + spBebette);

        string spPolch = KoreanPatch.TranslateSpeakerName("Polchinelle");
        Assert("Speaker Polchinelle", spPolch == "폴치넬레", "Got: " + spPolch);

        string spCassandro = KoreanPatch.TranslateSpeakerName("Cassandro");
        Assert("Speaker Cassandro", spCassandro == "카산드로", "Got: " + spCassandro);

        string spScapin = KoreanPatch.TranslateSpeakerName("Scapin");
        Assert("Speaker Scapin", spScapin == "스카팽", "Got: " + spScapin);

        string spValere = KoreanPatch.TranslateSpeakerName("Valère");
        Assert("Speaker Valère", spValere == "발레르", "Got: " + spValere);

        string spMetaphraste = KoreanPatch.TranslateSpeakerName("Metaphraste");
        Assert("Speaker Metaphraste", spMetaphraste == "메타프라스트", "Got: " + spMetaphraste);

        string spMezzetin = KoreanPatch.TranslateSpeakerName("Mezzetin");
        Assert("Speaker Mezzetin", spMezzetin == "메즈탱", "Got: " + spMezzetin);

        string spMopsy = KoreanPatch.TranslateSpeakerName("Aunt Mopsy");
        Assert("Speaker Aunt Mopsy", spMopsy == "몹시 이모", "Got: " + spMopsy);

        // 4. New Items
        string itemHourglass = KoreanPatch.Translate("Conclusive Hourglass");
        Assert("Item Conclusive Hourglass", itemHourglass == "결정적 모래시계", "Got: " + itemHourglass);

        string itemFlute = KoreanPatch.Translate("Pale Flute");
        Assert("Item Pale Flute", itemFlute == "창백한 피리", "Got: " + itemFlute);

        string itemBelt = KoreanPatch.Translate("Smuggler's Belt");
        Assert("Item Smuggler's Belt", itemBelt == "밀수꾼의 허리띠", "Got: " + itemBelt);

        string itemStolenYear = KoreanPatch.Translate("Stolen Year");
        Assert("Item Stolen Year", itemStolenYear == "도둑맞은 세월", "Got: " + itemStolenYear);

        // 5. New Outfits & Orders
        string orderAfterSix = KoreanPatch.Translate("Order: After Six");
        Assert("Order: After Six", orderAfterSix == "주문서: 애프터 식스", "Got: " + orderAfterSix);

        string pkgAfterSix = KoreanPatch.Translate("Package: After Six");
        Assert("Package: After Six", pkgAfterSix == "소포: 애프터 식스", "Got: " + pkgAfterSix);

        // 6. New Dialogue & Menu
        string menuWind = KoreanPatch.TranslateDialogue("The wind snatches the calendrier and carries it away - higher, higher.");
        Assert("Menu Wind Calendrier", menuWind.Contains("바람이 달력"), "Got: " + menuWind);

        string diagPancrata = KoreanPatch.TranslateDialogue("They made it at the convent. Yes.");
        Assert("Dialogue Convent", diagPancrata == "수도원에서 만든 겁니다. 그래요.", "Got: " + diagPancrata);

        string diagKingStag = KoreanPatch.TranslateDialogue("Wait! This sstag is our king!");
        Assert("Dialogue King Stag", diagKingStag == "잠깐! 이 수수사슴은 우리의 왕이다!", "Got: " + diagKingStag);

        Console.WriteLine("\n==============================");
        Console.WriteLine(string.Format("TOTAL: {0} PASSED, {1} FAILED", passed, failed));
        Console.WriteLine("==============================");
        if (failed > 0) Environment.Exit(1);
    }
}
