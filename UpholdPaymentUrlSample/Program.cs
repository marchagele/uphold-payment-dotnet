using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UpholdPaymentUrlSample
{
  class Program
  {
    static void Main(string[] args)
    {

      string paymentUser = "_user_name_which_should_do_the_payment";
      string paymentUrl = createPaymentUrl(paymentUser, 1, "USD");
      Process.Start("Chrome.exe", paymentUrl);
    }



    static string createPaymentUrl(string paymentUser, decimal amount, string currency)
    {
      TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
      long secondsSinceEpoch = (int)t.TotalSeconds;

      string upholdClientId = "YOUR_UPHOLD_CLIENT_ID";
      string upholdSecret = "YOUR_UPHOLD_SECRET";
      string specificCardId = "SPECIFIC_CARD_ID_ON_WHICH_TO_BOOK_THE_PAYMENT_ON_YOUR_APP";
     
      string destination = upholdClientId; //Default card on uphold.

      //with destination = upholdClientId it works.
      //but if you put destination = specificCardId then it fails.

      //to reproduce error just uncomment this line:
      destination = specificCardId;

      var header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
      var upholdClaims = "{\"iat\":" + secondsSinceEpoch + ",\"issuer\":\"" + upholdClientId + "\"," +
                            "\"transaction\":" +
                            "{\"destination\":\"" + destination + "\"," +
                              "\"title\": \"Buggy payment url with destination card id: " + destination + "\"," +
                              "\"denomination\":{\"amount\":" + amount.ToString().Replace(",", ".") + ",\"currency\":\"" + currency + "\"}" +
                            "}," +
                            "\"user\":{\"username\":\"" + paymentUser + "\"}}";


      var b64header = base64urlencode(Encoding.UTF8.GetBytes(header));
      var b64claims = base64urlencode(Encoding.UTF8.GetBytes(upholdClaims));
      var payload = b64header + "." + b64claims;
      var sig = CreateToken(payload, upholdSecret);

      string redirectUrl = "https://you-redirect-url";


      string qParam = "?redirectTo=" + redirectUrl + "&token=​" + payload + "." + sig + "";
      qParam = qParam.Replace("\u200B", ""); //Zero Length char workaround.
      string url = "https://gateway-sandbox.uphold.com/v0/pay" + qParam;

      return url;

    }

    static string CreateToken(string message, string secret)
    {
      secret = secret ?? "";
      var encoding = new System.Text.ASCIIEncoding();
      byte[] keyByte = encoding.GetBytes(secret);
      byte[] messageBytes = encoding.GetBytes(message);
      using (var hmacsha256 = new HMACSHA256(keyByte))
      {
        byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
        return base64urlencode(hashmessage);
      }
    }

    static string base64urlencode(byte[] arg)
    {
      string s = Convert.ToBase64String(arg); // Regular base64 encoder
      s = s.Split('=')[0]; // Remove any trailing '='s
      s = s.Replace('+', '-'); // 62nd char of encoding
      s = s.Replace('/', '_'); // 63rd char of encoding

      return s;
    }

    static string base64urldecode(string arg)
    {
      string s = arg;
      s = s.Replace('-', '+'); // 62nd char of encoding
      s = s.Replace('_', '/'); // 63rd char of encoding
      switch (s.Length % 4) // Pad with trailing '='s
      {
        case 0: break; // No pad chars in this case
        case 2: s += "=="; break; // Two pad chars
        case 3: s += "="; break; // One pad char
        default:
          throw new System.Exception(
   "Illegal base64url string!");
      }
      var base64EncodedBytes = System.Convert.FromBase64String(s);
      return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

  }
}
