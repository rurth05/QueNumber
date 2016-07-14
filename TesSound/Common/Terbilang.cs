using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Net.Mime;
using System.Windows.Forms;

namespace TesSound.Common
{
    class Terbilang
    {
        private static readonly
            string[] _satuan =
                new[]
                    {
                        "nol", "satu", "dua",
                        "tiga", "empat",
                        "lima", "enam", "tujuh",
                        "delapan", "sembilan"
                    };


        private static readonly
            string[] _belasan =
                new[]
                    {
                        "sepuluh", "sebelas", "dua",
                        "tiga", "empat",
                        "lima", "enam",
                        "tujuh",
                        "delapan", "sembilan"
                    };


        private static readonly
            string[] _puluhan =
                new[]
                    {
                        "", "", "dua puluh",
                        "tiga puluh", "empat puluh",
                        "lima puluh", "enam puluh",
                        "tujuh puluh", "delapan puluh",
                        "sembilan puluh"
                    };


        private static readonly
            string[] _ribuan =
                new[]
                    {
                        "", "ribu", "juta",
                        "milyar", "triliyun"
                    };

        public static void Suara(string str)
        {
            string[] txt;
            List<string> soundCol = new List<string>();
            var appPath = Application.StartupPath + "\\Sounds\\nomor-urut.wav";
            appPath = appPath.Replace("\\bin\\Debug", "");
            SoundPlayer player = new SoundPlayer(appPath);
            player.PlaySync();
            for (int i = str.Length; i > 0; i--)
            {
                //Get last digit
                var nDigit = Convert.ToInt32(str.Substring(i - 1, 1));
                
                var nPos = (str.Length - i) + 1;

                switch (nPos % 3)
                {
                    case 1:
                        bool bAllZeros = false;
                        string tmpBuff;

                        if (i == 1)
                        {// jika hanya 1 digit angka
                            tmpBuff = _satuan[nDigit];
                            appPath = Application.StartupPath + "\\Sounds\\" + tmpBuff + ".wav";
                            appPath = appPath.Replace("\\bin\\Debug", "");
                            soundCol.Add(appPath);
                        }
                        else if (str.Substring(i - 2, 1) == "1")
                        {
                            tmpBuff = _belasan[nDigit];
                            if (nDigit < 2)
                            {
                                appPath = Application.StartupPath + "\\Sounds\\" + tmpBuff + ".wav";
                                appPath = appPath.Replace("\\bin\\Debug", "");
                                soundCol.Add(appPath);
                            }
                            else
                            {
                                appPath = Application.StartupPath + "\\Sounds\\belas.wav";
                                appPath = appPath.Replace("\\bin\\Debug", "");
                                soundCol.Add(appPath);

                                appPath = Application.StartupPath + "\\Sounds\\" + tmpBuff + ".wav";
                                appPath = appPath.Replace("\\bin\\Debug", "");
                                soundCol.Add(appPath);
                            }
                        }
                        else if (nDigit > 0)
                        {
                            tmpBuff = _satuan[nDigit];
                            appPath = Application.StartupPath + "\\Sounds\\" + tmpBuff + ".wav";
                            appPath = appPath.Replace("\\bin\\Debug", "");
                            soundCol.Add(appPath);
                        }
                        break;
                    case 2:
                        if (nDigit > 1)
                        {
                            appPath = Application.StartupPath + "\\Sounds\\puluh.wav";
                            appPath = appPath.Replace("\\bin\\Debug", "");
                            soundCol.Add(appPath);

                            tmpBuff = _satuan[nDigit];
                            appPath = Application.StartupPath + "\\Sounds\\" + tmpBuff + ".wav";
                            appPath = appPath.Replace("\\bin\\Debug", "");
                            soundCol.Add(appPath);
                        }
                        break;
                    case 0:
                        if (nDigit < 2)
                        {
                            appPath = Application.StartupPath + "\\Sounds\\seratus.wav";
                            appPath = appPath.Replace("\\bin\\Debug", "");
                            soundCol.Add(appPath);
                        }
                        else
                        {
                            appPath = Application.StartupPath + "\\Sounds\\ratus.wav";
                            appPath = appPath.Replace("\\bin\\Debug", "");
                            soundCol.Add(appPath);

                            tmpBuff = _satuan[nDigit];
                            appPath = Application.StartupPath + "\\Sounds\\" + tmpBuff + ".wav";
                            appPath = appPath.Replace("\\bin\\Debug", "");
                            soundCol.Add(appPath);
                        }
                        break;
                }
            }

            for (int i = soundCol.Count - 1; i >= 0; i--)
            {
                player = new SoundPlayer(soundCol[i]);
                player.PlaySync();
            }
            appPath = Application.StartupPath + "\\Sounds\\loket.wav";
            appPath = appPath.Replace("\\bin\\Debug", "");
            player = new SoundPlayer(appPath);
            player.PlaySync();
        }

        public static string _Terbilang(string d)
        {
            string strResult = "";
            //Decimal frac = d - Decimal.Truncate(d);

            //if (Decimal.Compare(frac, 0.0m) != 0)
            //{
            //    strResult = _Terbilang(Decimal.Round(frac * 100)) + " sen";
            //}
            //else
            //{
            //    strResult = "rupiah";
            //}

            string strTemp = d;

            for (int i = strTemp.Length; i > 0; i--)
            {
                int nDigit = Convert.ToInt32(strTemp.Substring(i - 1, 1));
                int nPos = (strTemp.Length - i) + 1;
                switch (nPos % 3)
                {
                    case 1:
                        bool bAllZeros = false;
                        string tmpBuff;
                        if (i == 1)
                        {
                            tmpBuff = _satuan[nDigit] + " ";
                        }
                        else if (strTemp.Substring(i - 2, 1) == "1")
                        {
                            tmpBuff = _belasan[nDigit] + " ";
                        }
                        else if (nDigit > 0)
                        {
                            tmpBuff = _satuan[nDigit];
                            string appPath = string.Empty;
                            appPath = Application.StartupPath + "\\Sounds\\" + tmpBuff + ".wav";
                            appPath = appPath.Replace("\\bin\\Debug", "");
                            SoundPlayer player = new SoundPlayer(appPath);
                            player.PlaySync();
                        }
                        else
                        {
                            bAllZeros = true;
                            if (i > 1)
                            {
                                if (strTemp.Substring(i - 2, 1) != "0")
                                {
                                    bAllZeros = false;
                                }
                            }
                            if (i > 2)
                            {
                                if (strTemp.Substring(i - 3, 1) != "0")
                                {
                                    bAllZeros = false;
                                }
                            }
                            tmpBuff = "";
                        }

                        if ((!bAllZeros) && (nPos > 1))
                        {
                            if ((strTemp.Length == 4) &&
                            (strTemp.Substring(0, 1) == "1"))
                            {
                                tmpBuff =
                                "se" +
                                _ribuan[(int)Decimal.Round(nPos / 3m)] + " ";
                            }
                            else
                            {
                                tmpBuff +=
                                _ribuan[(int)Decimal.Round(nPos / 3)] + " ";
                            }
                        }
                        strResult = tmpBuff + strResult;
                        break;
                    case 2:
                        if (nDigit > 0)
                        {
                            strResult =
                            _puluhan[nDigit] + " " + strResult;
                        }
                        break;
                    case 0:
                        if (nDigit > 0)
                        {
                            if (nDigit == 1)
                            {
                                strResult =
                                "seratus " + strResult;
                            }
                            else
                            {
                                strResult =
                                _satuan[nDigit] + " ratus" + strResult;
                            }
                        }
                        break;
                }
            }

            strResult = strResult.Trim().ToLower();

            if (strResult.Length > 0)
            {
                strResult = strResult.Substring(0, 1).ToUpper() +
                strResult.Substring(1, strResult.Length - 1);
            }

            return strResult;
        }
    }
}
