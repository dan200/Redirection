using Dan200.Core.Computer.Devices;
using Dan200.Core.Lua;
using System;
using System.IO;
using System.Text;

namespace Dan200.Core.Computer.APIs
{
    public class OSAPI : LuaAPI
    {
        public static readonly DateTime ZeroTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static double TimeFromDate(DateTime time)
        {
            return (time.ToUniversalTime() - ZeroTime).TotalSeconds;
        }

        public static DateTime DateFromTime(double seconds)
        {
            try
            {
                return ZeroTime + TimeSpan.FromSeconds(seconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new LuaError("Invalid time");
            }
        }

        private Computer m_computer;
        private FileSystem m_fileSystem;
        private LuaCPUDevice m_cpu;

        public OSAPI(Computer computer, LuaCPUDevice cpu, FileSystem fileSystem) : base("os")
        {
            m_computer = computer;
            m_cpu = cpu;
            m_fileSystem = fileSystem;
        }

        [LuaMethod]
        public LuaArgs clock(LuaArgs args)
        {
            var clockDevice = FindClock();
            if (clockDevice != null)
            {
                return new LuaArgs(clockDevice.Clock.TotalSeconds);
            }
            else
            {
                return new LuaArgs(0.0);
            }
        }

        [LuaMethod]
        public LuaArgs date(LuaArgs args)
        {
            string format;
            if (!args.IsNil(0))
            {
                format = args.GetString(0);
            }
            else
            {
                format = "%c";
            }

            DateTime time;
            if (!args.IsNil(1))
            {
                var seconds = args.GetDouble(1);
                time = DateFromTime(seconds);
            }
            else
            {
                var clockDevice = FindClock();
                if (clockDevice != null)
                {
                    time = clockDevice.Time;
                }
                else
                {
                    time = DateFromTime(0.0);
                }
            }
            if (format.StartsWith("!", StringComparison.InvariantCulture))
            {
                time = time.ToUniversalTime();
                format = format.Substring(1);
            }
            else
            {
                time = time.ToLocalTime();
            }
            if (format.Equals("*t"))
            {
                var result = new LuaTable(10);
                result["year"] = new LuaValue(time.Year);
                result["month"] = new LuaValue(time.Month);
                result["day"] = new LuaValue(time.Day);
                result["hour"] = new LuaValue(time.Hour);
                result["min"] = new LuaValue(time.Minute);
                result["sec"] = new LuaValue(time.Second);
                result["wday"] = new LuaValue((int)time.DayOfWeek + 1);
                result["yday"] = new LuaValue(time.DayOfYear);
                result["isdst"] = new LuaValue(time.IsDaylightSavingTime());
                result["isutc"] = new LuaValue(time.Kind == DateTimeKind.Utc);
                return new LuaArgs(result);
            }
            else
            {
                try
                {
                    return new LuaArgs(StrFTime(time, format));
                }
                catch (FormatException e)
                {
                    throw new LuaError(e.Message);
                }
            }
        }

        [LuaMethod]
        public LuaArgs difftime(LuaArgs args)
        {
            var t2 = args.GetDouble(0);
            var t1 = args.GetDouble(1);
            return new LuaArgs(t2 - t1);
        }

        [LuaMethod]
        public LuaArgs execute(LuaArgs args)
        {
            var command = args.IsNil(0) ? null : args.GetString(0);
            if (command != null)
            {
                return LuaArgs.Nil;
            }
            else
            {
                return new LuaArgs(false);
            }
        }

        [LuaMethod]
        public LuaArgs exit(LuaArgs args)
        {
			if (!args.IsNil(0) && !args.IsBool(0)) {
				args.GetInt(0);
			}
            m_cpu.RequestShutdown();
			throw new LuaYield(LuaArgs.Empty, delegate {
				return LuaArgs.Empty;
			});
        }

        [LuaMethod]
        public LuaArgs getenv(LuaArgs args)
        {
            args.GetString(0);
            return LuaArgs.Nil;
        }

        [LuaMethod]
        public LuaArgs remove(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            try
            {
                m_fileSystem.Delete(path);
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs rename(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            var dest = new FilePath(args.GetString(1));
            try
            {
                m_fileSystem.Move(path, dest);
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs setlocale(LuaArgs args)
        {
            if (args.IsNil(0))
            {
                if (!args.IsNil(1))
                {
                    args.GetString(1);
                }
                return new LuaArgs("C");
            }
            else
            {
                args.GetString(0);
                if (!args.IsNil(1))
                {
                    args.GetString(1);
                }
                return LuaArgs.Empty;
            }
        }

        [LuaMethod]
        public LuaArgs time(LuaArgs args)
        {
            DateTime dateTime;
            if (args.IsNil(0))
            {
                var clockDevice = FindClock();
                if (clockDevice != null)
                {
                    dateTime = clockDevice.Time;
                }
                else
                {
                    dateTime = DateFromTime(0.0);
                }
            }
            else
            {
                var table = args.GetTable(0);
                int year = table.GetInt("year");
                int month = table.GetInt("month");
                int day = table.GetInt("day");
                int hour = table.IsNil("hour") ? 12 : table.GetInt("hour");
                int min = table.IsNil("min") ? 0 : table.GetInt("min");
                int sec = table.IsNil("sec") ? 0 : table.GetInt("sec");
                if (!table.IsNil("isdst")) table.GetBool("isdst");
                bool isutc = table.IsNil("isutc") ? false : table.GetBool("isutc");
                try
                {
                    dateTime = new DateTime(year, month, day, hour, min, sec, isutc ? DateTimeKind.Utc : DateTimeKind.Local);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new LuaError("Invalid date");
                }
            }
            var seconds = TimeFromDate(dateTime);
            return new LuaArgs(seconds);
        }

        [LuaMethod]
        public LuaArgs tmpname(LuaArgs args)
        {
            try
            {
                FilePath path;
                int i = 0;
                while (m_fileSystem.Exists(path = new FilePath("tmp/" + i + ".tmp")))
                {
                    ++i;
                }
                m_fileSystem.MakeDir(path.GetDir());
                m_fileSystem.OpenForWrite(path, false).Close();
                return new LuaArgs(path.ToString());
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        private ClockDevice FindClock()
		{
			return m_computer.Devices["clock"] as ClockDevice;
		}

		private static string StrFTime(DateTime t, string format)
        {
            var builder = new StringBuilder();
            StrFTimeImpl(t, format, builder);
            return builder.ToString();
        }

        private static void StrFTimeImpl(DateTime t, string format, StringBuilder output)
        {
            for (int i = 0; i < format.Length; ++i)
            {
                var c = getChar(format, i);
                if (c == '%')
                {
                    var fmtStart = i;
                    c = getChar(format, ++i);
                    if (c == 'E')
                    {
                        c = getChar(format, ++i); // Alternate Era is ignored
                    }
                    else if (c == 'O')
                    {
                        c = getChar(format, ++i); // Alternate numeric symbols is ignored
                    }

                    switch (c)
                    {
                        case 'A': // Full day of week
                                  //pt = _add((t->tm_wday < 0 || t->tm_wday > 6) ? "?" : _days[t->tm_wday], pt, ptlim);
                            output.Append(t.ToString("dddd"));
                            continue;

                        case 'a': // Abbreviated day of week
                                  //pt = _add((t->tm_wday < 0 || t->tm_wday > 6) ? "?" : _days_abbrev[t->tm_wday], pt, ptlim);
                            output.Append(t.ToString("ddd"));
                            continue;

                        case 'B': // Full month name
                                  //pt = _add((t->tm_mon < 0 || t->tm_mon > 11) ? "?" : _months[t->tm_mon], pt, ptlim);
                            output.Append(t.ToString("MMMM"));
                            continue;

                        case 'b': // Abbreviated month name
                        case 'h': // Abbreviated month name
                                  //pt = _add((t->tm_mon < 0 || t->tm_mon > 11) ? "?" : _months_abbrev[t->tm_mon], pt, ptlim);
                            output.Append(t.ToString("MMM"));
                            continue;

                        case 'C': // First two digits of year (a.k.a. Year divided by 100 and truncated to integer (00-99))
                                  //pt = _conv((t->tm_year + TM_YEAR_BASE) / 100, "%02d", pt, ptlim);
                            output.Append(t.ToString("yyyy").Substring(0, 2));
                            continue;

                        case 'c': // Abbreviated date/time representation (e.g. Thu Aug 23 14:55:02 2001)
                            StrFTimeImpl(t, "%a %b %e %H:%M:%S %Y", output);
                            continue;

                        case 'D': // Short MM/DD/YY date
                            StrFTimeImpl(t, "%m/%d/%y", output);
                            continue;

                        case 'd': // Day of the month, zero-padded (01-31)
                                  //pt = _conv(t->tm_mday, "%02d", pt, ptlim);
                            output.Append(t.ToString("dd"));
                            continue;

                        case 'e': // Day of the month, space-padded ( 1-31)
                                  //pt = _conv(t->tm_mday, "%2d", pt, ptlim);
                            output.Append(t.Day.ToString().PadLeft(2, ' '));
                            continue;

                        case 'F': // Short YYYY-MM-DD date
                            StrFTimeImpl(t, "%Y-%m-%d", output);
                            continue;

                        case 'H': // Hour in 24h format (00-23)
                                  //pt = _conv(t->tm_hour, "%02d", pt, ptlim);
                            output.Append(t.ToString("HH"));
                            continue;

                        case 'I': // Hour in 12h format (01-12)
                                  //pt = _conv((t->tm_hour % 12) ? (t->tm_hour % 12) : 12, "%02d", pt, ptlim);
                            output.Append(t.ToString("hh"));
                            continue;

                        case 'j': // Day of the year (001-366)
                            output.Append(t.DayOfYear.ToString().PadLeft(3, ' '));
                            continue;

                        case 'k': // (Non-standard) // Hours in 24h format, space-padded ( 1-23)
                                  //pt = _conv(t->tm_hour, "%2d", pt, ptlim);
                            output.Append(t.ToString("%H").PadLeft(2, ' '));
                            continue;

                        case 'l': // (Non-standard) // Hours in 12h format, space-padded ( 1-12)
                                  //pt = _conv((t->tm_hour % 12) ? (t->tm_hour % 12) : 12, "%2d", pt, ptlim);
                            output.Append(t.ToString("%h").PadLeft(2, ' '));
                            continue;

                        case 'M': // Minute (00-59)
                                  //pt = _conv(t->tm_min, "%02d", pt, ptlim);
                            output.Append(t.ToString("mm"));
                            continue;

                        case 'm': // Month as a decimal number (01-12)
                                  //pt = _conv(t->tm_mon + 1, "%02d", pt, ptlim);
                            output.Append(t.ToString("MM"));
                            continue;

                        case 'n': // New-line character.
                            output.Append('\n');
                            continue;

                        case 'p': // AM or PM designation (locale dependent).
                                  //pt = _add((t->tm_hour >= 12) ? "pm" : "am", pt, ptlim);
                            output.Append(t.ToString("tt"));
                            continue;

                        case 'R': // 24-hour HH:MM time, equivalent to %H:%M
                            StrFTimeImpl(t, "%H:%M", output);
                            continue;

                        case 'r': // 12-hour clock time (locale dependent).
                            StrFTimeImpl(t, "%I:%M:%S %p", output);
                            continue;

                        case 'S': // Second ((00-59)
                                  //pt = _conv(t->tm_sec, "%02d", pt, ptlim);
                            output.Append(t.ToString("ss"));
                            continue;

                        case 'T': // ISO 8601 time format (HH:MM:SS), equivalent to %H:%M:%S
                            StrFTimeImpl(t, "%H:%M:%S", output);
                            continue;

                        case 't': // Horizontal-tab character
                            output.Append('\t');
                            continue;

                        case 'U': // Week number with the first Sunday as the first day of week one (00-53)
                                  //pt = _conv((t->tm_yday + 7 - t->tm_wday) / 7, "%02d", pt, ptlim);
                            output.Append(System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(t, System.Globalization.CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday).ToString());
                            continue;

                        case 'u': // ISO 8601 weekday as number with Monday as 1 (1-7) (locale independant).
                                  //pt = _conv((t->tm_wday == 0) ? 7 : t->tm_wday, "%d", pt, ptlim);
                            output.Append(t.DayOfWeek == DayOfWeek.Sunday ? "7" : ((int)t.DayOfWeek).ToString());
                            continue;

                        case 'G':   // ISO 8601 year (four digits)
                        case 'g':  // ISO 8601 year (two digits)
                        case 'V':   // ISO 8601 week number
                                    // See http://stackoverflow.com/questions/11154673/get-the-correct-week-number-of-a-given-date
                            DateTime isoTime = t;
                            DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(isoTime);
                            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
                            {
                                isoTime = isoTime.AddDays(3);
                            }

                            if (c == 'V') // ISO 8601 week number
                            {
                                int isoWeek = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(isoTime, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                                output.Append(isoWeek.ToString());
                            }
                            else
                            {
                                string isoYear = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetYear(isoTime).ToString(); // ISO 8601 year (four digits)
                                if (c == 'g') // ISO 8601 year (two digits)
                                {
                                    isoYear = isoYear.Substring(isoYear.Length - 2, 2);
                                }
                                output.Append(isoYear);
                            }

                            continue;

                        case 'W': // Week number with the first Monday as the first day of week one (00-53)
                                  //pt = _conv((t->tm_yday + 7 - (t->tm_wday ? (t->tm_wday - 1) : 6)) / 7, "%02d", pt, ptlim);
                            output.Append(System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(t, System.Globalization.CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday).ToString());
                            continue;

                        case 'w': // Weekday as a decimal number with Sunday as 0 (0-6)
                                  //pt = _conv(t->tm_wday, "%d", pt, ptlim);
                            output.Append(((int)t.DayOfWeek).ToString());
                            continue;

                        case 'X': // Long time representation (locale dependent)
                                  //pt = _fmt("%H:%M:%S", t, pt, ptlim); // fails to comply with spec!
                            output.Append(t.ToString("%T"));
                            continue;

                        case 'x': // Short date representation (locale dependent)
                                  //pt = _fmt("%m/%d/%y", t, pt, ptlim); // fails to comply with spec!
                            output.Append(t.ToString("%d"));
                            continue;

                        case 'y': // Last two digits of year (00-99)
                                  //pt = _conv((t->tm_year + TM_YEAR_BASE) % 100, "%02d", pt, ptlim);
                            output.Append(t.ToString("yy"));
                            continue;

                        case 'Y': // Full year (all digits)
                                  //pt = _conv(t->tm_year + TM_YEAR_BASE, "%04d", pt, ptlim);
                            output.Append(t.Year.ToString());
                            continue;

                        case 'Z': // Timezone name or abbreviation (locale dependent) or nothing if unavailable (e.g. CDT)
                            if (t.Kind == DateTimeKind.Utc)
                            {
                                output.Append(TimeZoneInfo.Utc.StandardName);
                            }
                            else
                            {
                                var timezone = TimeZoneInfo.Local;
                                if (t.IsDaylightSavingTime())
                                {
                                    output.Append(TimeZoneInfo.Local.DaylightName);
                                }
                                else
                                {
                                    output.Append(TimeZoneInfo.Local.StandardName);
                                }
                            }
                            continue;

                        case 'z': // ISO 8601 offset from UTC in timezone (+/-hhmm), or nothing if unavailable
                            if (t.Kind == DateTimeKind.Utc)
                            {
                                output.Append("+0000");
                            }
                            else
                            {
                                TimeSpan ts = TimeZoneInfo.Local.GetUtcOffset(t);
                                string offset = (ts.Ticks < 0 ? "-" : "+") + ts.TotalHours.ToString("#00") + ts.Minutes.ToString("00");
                                output.Append(offset);
                            }
                            continue;

                        case '%': // Add '%'
                            output.Append('%');
                            continue;

                        default:
                            throw new FormatException("Invalid conversion specifier: " + format.Substring(fmtStart, Math.Min(i + 1, format.Length) - fmtStart));
                    }
                }
                else
                {
                    output.Append(c);
                }
            }
        }

        private static char getChar(string str, int i)
        {
            if (i < 0 || i >= str.Length)
            {
                return '\0';
            }
            else
            {
                return str[i];
            }
        }
    }
}
