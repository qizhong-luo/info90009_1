using System.Text.RegularExpressions;

namespace Sleepet
{
    // Local, deterministic intent routing works identically with either model provider.
    public static class CompanionSkillRouter
    {
        public static string Resolve(string message, string previous = CompanionSkills.Daily)
        {
            string text = (message ?? "").Trim().ToLowerInvariant();
            if (Has(text, @"\b(tomorrow|plan|plans|planned|planning|schedule|scheduled|agenda|prepare|preparation)\b|明天|明日|计划|安排|日程|准备"))
                return CompanionSkills.Tomorrow;
            if (Has(text, @"\b(records?|history|sessions?|recorded|yesterday|last night|last week|past week|seven days|7 days|review|trends?|slept)\b|记录|回顾|统计|昨晚|昨天|过去|上周|近七天|最近.{0,6}睡|睡了多久"))
                return CompanionSkills.Records;
            if (Has(text, @"\b(wake|alarm|reminder|preference|preferences|hello|hi|hey|tired|lonely|sad|company|feel|feeling)\b|起床|闹钟|提醒|偏好|你好|累|难过|陪|心情"))
                return CompanionSkills.Daily;
            if (Has(text, @"^(why|how long|what about|and |tell me more|explain|which|how many|what else)|为什么|多久|还有|详细|然后|哪些|多少"))
                return previous;
            return CompanionSkills.Daily;
        }
        static bool Has(string text, string pattern) => Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant);
    }
}
