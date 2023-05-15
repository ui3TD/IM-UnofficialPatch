using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UnofficialPatch
{

    [HarmonyPatch(typeof(Profile_Fans_Pies), "Render_Pies")]
    public class Profile_Fans_Pies_Render_Pies
    {

        // Fixed fan pie charts so that it is correct
        public static void Postfix(Profile_Fans_Pies __instance)
        {
            //NotificationManager.AddNotification("postfix applied", mainScript.green32, NotificationManager._notification._type.other);
            data_girls.girls Girl = Traverse.Create(__instance).Field("Girl").GetValue() as data_girls.girls;

            if (Girl.GetFans_Total(null) != 0L)
            {
                __instance.Fans_Pie_Adult.GetComponent<Image>().fillAmount += __instance.Fans_Pie_YA.GetComponent<Image>().fillAmount - __instance.Fans_Pie_Teen.GetComponent<Image>().fillAmount;
            }
        }

    }

    [HarmonyPatch(typeof(Tour_New_Popup), "Render")]
    public class Tour_New_Popup_Render
    {
        // Fixed tour revenue text color to consider savings
        public static void Postfix(ref Tour_New_Popup __instance)
        {
            //NotificationManager.AddNotification("postfix applied", mainScript.green32, NotificationManager._notification._type.other);

            if (__instance.Tour.ExpectedRevenue > __instance.Tour.ProductionCost - __instance.Tour.Saving)
            {
                ExtensionMethods.SetColor(__instance.ExpectedRevenue, mainScript.green32);
            }

        }
    }

    [HarmonyPatch(typeof(Theaters), "GetStaminaCost")]
    public class Theaters_GetStaminaCost
    {
        // Fixed Theater so that it uses stamina.
        public static void Postfix(Theaters._theater._schedule._type Type, ref float __result)
        {
            float num = 0f;
            if (Type == Theaters._theater._schedule._type.performance)
            {
                num = 5f;
            }
            else if (Type == Theaters._theater._schedule._type.manzai)
            {
                num = 2f;
            }
            if (staticVars.IsHard())
            {
                num *= 2f;
            }
            __result = num;
        }
    }

    [HarmonyPatch(typeof(Theaters), "CompleteDay")]
    public class Theaters_CompleteDay
    {
        // Fixed Theater so that revenue stats are not offset by one day
        public static bool Prefix()
        {
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                // Fix so that auto schedules contribute revenue on the day of
                theater.Doing_Now = theater.GetSchedule().Type;
            }
            return true;
        }


        // Fixed Theater so that average stats ignore days off, and so that girls earnings are increased by revenue
        public static void Postfix()
        {
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                // Fix so that auto schedules contribute revenue on the day of
                if (theater.GetSchedule().Type == Theaters._theater._schedule._type.auto &&
                    (theater.Doing_Now == Theaters._theater._schedule._type.manzai || theater.Doing_Now == Theaters._theater._schedule._type.performance))
                {
                    long rev = theater.GetTicketSales();
                    if (rev > 0) resources.Add(resources.type.money, rev);
                    if (staticVars.dateTime.Day != 1)
                    {
                        theater.GetRoom().addFloat(Floats.type.icon_money, "", true, null, 0f, 1f, 0f, null);
                    }
                }

                // Fix so that days off have no revenue
                if (theater.Doing_Now == Theaters._theater._schedule._type.day_off)
                {
                    theater.Stats[theater.Stats.Count - 1].Revenue = 0;
                }

                if (theater.Doing_Now == Theaters._theater._schedule._type.performance || theater.Doing_Now == Theaters._theater._schedule._type.manzai)
                {
                    int num2 = theater.GetGroup().GetGirls(true, false, null).Count;
                    long num = theater.GetTicketSales();
                    if (theater.AreSubsUnlocked() && staticVars.dateTime.Day == 1)
                    {
                        num += theater.GetSubRevenue();
                    }
                    foreach (data_girls.girls girls2 in theater.GetGroup().GetGirls(true, false, null))
                    {
                        if (num > 0L && num2 > 0)
                        {
                            girls2.Earn(num / (long)num2);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Theaters._theater), "GetAvgAttendance")]
    public class Theaters__theater_GetAvgAttendance
    {
        // Fixed Theater so that average stats ignore days off
        public static void Postfix(ref int __result, Theaters._theater __instance)
        {
            float num = 0f;
            float num2 = 7f;
            if (__instance.Stats.Count == 0)
            {
                __result = 0;
            }
            if (__instance.Stats.Count < 7)
            {
                num2 = (float)__instance.Stats.Count;
            }
            int num3 = __instance.Stats.Count - 1;
            int num4 = 0;
            while ((float)num3 >= (float)__instance.Stats.Count - num2)
            {
                if (__instance.Stats[num3].Schedule.Type != Theaters._theater._schedule._type.day_off)
                {
                    num += (float)__instance.Stats[num3].Attendance;
                    num4++;
                }
                num3--;
            }
            if (num4 != 0)
            {
                num /= (float)num4;
            }
            __result = Mathf.RoundToInt(num);
        }

    }

    [HarmonyPatch(typeof(Theaters._theater), "GetAvgRevenue")]
    public class Theaters__theater_GetAvgRevenue
    {
        // Fixed Theater so that average stats ignore days off
        public static void GetAvgRevenuePostfix(ref int __result, Theaters._theater __instance)
        {
            float num = 0f;
            float num2 = 7f;
            if (__instance.Stats.Count == 0)
            {
                __result = 0;
            }
            if (__instance.Stats.Count < 7)
            {
                num2 = (float)__instance.Stats.Count;
            }
            int num3 = __instance.Stats.Count - 1;
            int num4 = 0;
            while ((float)num3 >= (float)__instance.Stats.Count - num2)
            {
                if (__instance.Stats[num3].Schedule.Type != Theaters._theater._schedule._type.day_off)
                {
                    num += (float)__instance.Stats[num3].Revenue;
                    num4++;
                }
                num3--;
            }
            if (num4 != 0)
            {
                num /= (float)num4;
            }
            __result = Mathf.RoundToInt(num);
        }
    }


    [HarmonyPatch(typeof(Theaters), "GetLastWeekEarning")]
    public class Theaters_GetLastWeekEarning
    {
        // Fixed Theater so that money tooltip includes 7 days instead of 6, and include sub revenue
        public static void Postfix(ref long __result)
        {
            long output = __result;
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                if (theater.Stats.Count >= 7)
                {
                    output += theater.Stats[theater.Stats.Count - 7].Revenue;
                }
                if (theater.AreSubsUnlocked())
                {
                    output += theater.GetSubRevenue();
                }
            }
            __result = output;
        }
    }

    [HarmonyPatch(typeof(Cafes), "GetLastWeekEarning")]
    public class Cafes_GetLastWeekEarning
    {
        // Fixed Cafe so that money tooltip includes 7 days instead of 6
        public static void Postfix(ref int __result)
        {
            int output = __result;
            foreach (Cafes._cafe cafe in Cafes.Cafes_)
            {
                if (cafe.Stats.Count >= 7)
                {
                    output += cafe.Stats[cafe.Stats.Count - 7].Profit;
                }
            }
            __result = output;
        }
    }


    [HarmonyPatch(typeof(Relationships._relationship), "BreakUp")]
    public class Relationships__relationship_BreakUp
    {
        // Fixed so that when girls dating within the group break up, their relationship status is no longer known
        public static void Postfix(ref Relationships._relationship __instance)
        {
            if (__instance.Dating)
            {
                __instance.Girls[0].DatingData.Is_Partner_Status_Known = false;
                __instance.Girls[1].DatingData.Is_Partner_Status_Known = false;
            }
        }
    }

    [HarmonyPatch(typeof(SEvent_Concerts._concert._projectedValues), "GetRevenue")]
    public class SEvent_Concerts__concert__projectedValues_GetRevenue
    {
        // Fixed Concert revenue formula so that it shows accurate estimated values
        public static void Postfix(ref long __result, SEvent_Concerts._concert._projectedValues __instance)
        {
            long output = __result;

            float hype = __instance.GetHype() * 100f;
            if (hype > 100f)
            {
                float num;
                float num2 = hype - 100f;
                LinearFunction._function function = new LinearFunction._function();
                function.Init(0f, 50f, 100f, 25f);
                float num3 = function.GetY(num2) / 100f;
                num = num2 * num3 + 100f;
                output = (long)Mathf.Round(output / hype * num);
            }

            __result = output;
        }
    }


    [HarmonyPatch(typeof(SEvent_Concerts._concert._projectedValues), "GetString")]
    public class SEvent_Concerts__concert__projectedValues_GetString
    {
        // Fixed Concert revenue formula so that it shows accurate estimated values
        public static bool Prefix(ref float _val)
        {
            if(_val >= 99.5)
            {
                _val = 99;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(singles._single), "SenbatsuCalcParam")]
    public class singles__single_SenbatsuCalcParam
    {
        // Fixed senbatsu stats calculation so it doesn't punish you if you don't have enough idols to fill all rows
        public static void Postfix(Groups._group Group, singles._single __instance, ref data_girls.girls.param __result )
        {
            if (Group == null)
            {
                Group = __instance.GetGroup();
            }
            int num = Group.GetNumberOfNonGraduatedGirls();
            if (Group.IsMain())
            {
                foreach (Groups._group group in Groups.Groups_)
                {
                    if (group != Group)
                    {
                        num += group.GetNumberOfNonGraduatedGirls();
                    }
                }
            }
            float num2_orig = 5;
            float num2 = 5;
            if (num == 0)
            {
                return;
            }
            else if (num == 1)
            {
                return;
            }
            else if (num <= 3)
            {
                num2_orig = 2;
                num2 = 1 + (num - 1) / 2;
            }
            else if (num <= 6)
            {
                num2_orig = 3;
                num2 = 2 + (num - 3) / 3;
            }
            else if (num <= 10)
            {
                num2_orig = 4;
                num2 = 3 + (num - 6) / 4;
            }
            else if (num <= 15)
            {
                num2_orig = 5;
                num2 = 3 + (num - 10) / 5;
            }
            float num3 = 100f / num2;
            float num3_orig = 100f / num2_orig;
            float coeff = num3 / num3_orig;

            float val_capped = __result.val * coeff;
            if (val_capped > 100f)
            {
                val_capped = 100f;
            }
            data_girls.girls.param output = new data_girls.girls.param
            {
                type = __result.type,
                val = val_capped
            };
            __result = output;
        }
    }

}
