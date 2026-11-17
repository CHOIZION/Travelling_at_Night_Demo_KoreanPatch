using System;
using System.IO;
using Travelling.Korean;

class VerifyV11
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

        // 1. Knock = 두드림
        string trKnock = KoreanPatch.Translate("Knock");
        Assert("Knock in Translate", trKnock == "두드림", "Got: " + trKnock);

        string locKnock = KoreanPatch.TranslateLocKey("Knock");
        Assert("Knock in TranslateLocKey", locKnock == "두드림", "Got: " + locKnock);

        // 2. Invisible Bag = 보이지 않는 가방
        string trInvBag = KoreanPatch.Translate("Invisible Bag");
        Assert("Invisible Bag in Translate", trInvBag == "보이지 않는 가방", "Got: " + trInvBag);

        string locInvBag = KoreanPatch.TranslateLocKey("Invisible Bag");
        Assert("Invisible Bag in TranslateLocKey", locInvBag == "보이지 않는 가방", "Got: " + locInvBag);

        // 3. Why? = 어째서입니까?
        string trWhy = KoreanPatch.Translate("Why?");
        Assert("Why? dialogue tone", trWhy == "어째서입니까?", "Got: " + trWhy);

        // 4. TranslateAspectPhrase
        string ph1 = KoreanPatch.TranslateAspectPhrase("<sprite=5>Obscure, <sprite=2>Bewitch or <sprite=8>Trespass");
        Assert("Aspect phrase 3 items", ph1 == "<sprite=5>은폐, <sprite=2>홀림 또는 <sprite=8>침범", "Got: " + ph1);

        string ph2 = KoreanPatch.TranslateAspectPhrase("<sprite=1>Expel");
        Assert("Aspect phrase single Expel", ph2 == "<sprite=1>추방", "Got: " + ph2);

        string ph3 = KoreanPatch.TranslateAspectPhrase("Knock/Rose");
        Assert("Aspect slash Knock/Rose", ph3 == "두드림/장미", "Got: " + ph3);

        // 5. TranslateLocArgs with advice template
        string tmpl = "ADVICE_HAS_SKILL_MODIFIERS_EXHAUSTABLE";
        string[] args1 = new string[] { "<sprite=5>Obscure, <sprite=2>Bewitch or <sprite=8>Trespass" };
        KoreanPatch.TranslateLocArgs(ref tmpl, args1);
        Assert("TranslateLocArgs 3 aspects", args1[0] == "<sprite=5>은폐, <sprite=2>홀림 또는 <sprite=8>침범", "Got: " + args1[0]);

        string[] args2 = new string[] { "<sprite=1>Expel" };
        KoreanPatch.TranslateLocArgs(ref tmpl, args2);
        Assert("TranslateLocArgs Expel", args2[0] == "<sprite=1>추방", "Got: " + args2[0]);

        Console.WriteLine("\n==============================");
        Console.WriteLine(string.Format("TOTAL: {0} PASSED, {1} FAILED", passed, failed));
        Console.WriteLine("==============================");
        if (failed > 0) Environment.Exit(1);
    }
}
