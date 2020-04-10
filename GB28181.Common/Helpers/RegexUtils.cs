using System.Text.RegularExpressions;
namespace Helpers
{
	public class RegexUtils
	{
		public class Pattern
		{
			public static bool matches(string regex, string str)
			{
				Regex regex2 = new Regex(regex);
				int count = regex2.Matches(str).Count;
				return count > 0;
			}
		}

		public static bool checkEmail(string email)
		{
			string regex = "\\w+@\\w+\\.[a-z]+(\\.[a-z]+)?";
			string regex2 = "\\w+\\.[a-z]+(\\.[a-z]+)?@\\w+\\.[a-z]+(\\.[a-z]+)?";
			return Pattern.matches(regex, email) || Pattern.matches(regex2, email);
		}

		public static bool checkIdCard(string idCard)
		{
			string regex = "[1-9]\\d{13,16}[a-zA-Z0-9]{1}";
			return Pattern.matches(regex, idCard);
		}

		public static bool checkMobile(string mobile)
		{
			string regex = "(\\+\\d+)?1[3458]\\d{9}$";
			return Pattern.matches(regex, mobile);
		}

		public static bool checkPhone(string phone)
		{
			string regex = "(\\+\\d+)?(\\d{3,4}\\-?)?\\d{7,8}$";
			return Pattern.matches(regex, phone);
		}

		public static bool checkDigit(string digit)
		{
			string regex = "\\-?[1-9]\\d+";
			return Pattern.matches(regex, digit);
		}

		public static bool checkDecimals(string decimals)
		{
			string regex = "\\-?[1-9]\\d+(\\.\\d+)?";
			return Pattern.matches(regex, decimals);
		}

		public static bool checkBlankSpace(string blankSpace)
		{
			string regex = "\\s+";
			return Pattern.matches(regex, blankSpace);
		}

		public static bool checkChinese(string chinese)
		{
			string regex = "^[一-龥]+$";
			return Pattern.matches(regex, chinese);
		}

		public static bool checkBirthday(string birthday)
		{
			string regex = "[1-9]{4}([-./])\\d{1,2}\\1\\d{1,2}";
			return Pattern.matches(regex, birthday);
		}

		public static bool checkURL(string url)
		{
			string regex = "(https?://(w{3}\\.)?)?\\w+\\.\\w+(\\.[a-zA-Z]+)*(:\\d{1,5})?(/\\w*)*(\\??(.+=.*)?(&.+=.*)?)?";
			return Pattern.matches(regex, url);
		}

		public static bool checkPostcode(string postcode)
		{
			string regex = "[1-9]\\d{5}";
			return Pattern.matches(regex, postcode);
		}

		public static bool checkIpAddress(string ipAddress)
		{
			string regex = "[1-9](\\d{1,2})?\\.(0|([1-9](\\d{1,2})?))\\.(0|([1-9](\\d{1,2})?))\\.(0|([1-9](\\d{1,2})?))";
			return Pattern.matches(regex, ipAddress);
		}

		public static bool checkAccount(string account)
		{
			string regex = "[a-zA-Z0-9]{4,15}";
			return Pattern.matches(regex, account);
		}
	}

}