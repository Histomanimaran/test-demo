using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace BlastSearch
{
    public class BS
    {
        #region Function to be called from Client
        public static string BlastSearchSequence(string sAmpliconName, string sSequence, int iDatabasemasterID, string sTypingMethod, string Querypart, string sGeneName)
        {

            string sAlleleList = string.Empty;

            string targetSegments = string.Empty;
            if (sAmpliconName.IndexOf("~") > 0)
            {
                targetSegments = sAmpliconName.Substring(0, sAmpliconName.IndexOf('~'));
                sAmpliconName = sAmpliconName.Substring(sAmpliconName.IndexOf('~') + 1);
            }
            try
            {
                if (Querypart == "")
                {
                    Querypart = "NWAL01";
                }
                string sJSON = GenerateQueryForLRBLAST(sAmpliconName, sSequence, iDatabasemasterID, sTypingMethod, Querypart, targetSegments);
                if (sJSON != string.Empty)
                {
                    sAlleleList = BlastSearchPacbio(sJSON, Convert.ToString(iDatabasemasterID), sTypingMethod, sGeneName);
                }
                else
                {
                    //return "Error : Invalid Input"; 
                    return "";
                }
            }
            catch (Exception ex)
            {
                //return "Error : " + ex.Message;
                return "";
            }
            return sAlleleList;
        }
        #endregion

        #region Generate Query for BLAST
        private static string GenerateQueryForLRBLAST(string sAmpliconName, string sSequence, int iDatabasemasterID, string sTypingMethod, string Querypart, string targetSegments)
        {
            string sJSON = string.Empty;
            try
            {
                string sTargetSegment = string.Empty;

                if (sTypingMethod == "PacBio-FR" || sTypingMethod == "PacBio-R")
                {
                    sTargetSegment = "E1;E2;E3;E4;E5;E6;E7;E8";
                }
                else if (sTypingMethod == "PacBio-FREI")
                {
                    sTargetSegment = "5UTR;E1;I1;E2;I2;E3;I3;E4;I4;E5;I5;E6;I6;E7;I7;E8;3UTR";
                }
                else if (sTypingMethod.Contains("NGS"))
                {
                    sTargetSegment = sAmpliconName;
                }
                else if (sTypingMethod == "LR")
                {
                    sTargetSegment = targetSegments;
                }
                string sAmplicon = sAmpliconName;
                string sRawData1 = sSequence;

                string sUniqueName = "1" + "#" + sAmplicon + "--1#" + "NWAL01";
                sJSON = "[{\"id\":\"" + sUniqueName + "\",\"sequence\":\"" + sRawData1 + "\",\"quality\":\"" + sRawData1 + "\",\"target\":\"" + sTargetSegment + "\",\"status\":\"NWAL01\"}]";
            }
            catch (Exception ex)
            {
                return "";
            }
            return sJSON;
        }

        #endregion

        #region Call Blast Search Service
        private static string BlastSearchPacbio(string strJSON, string sGroupID, string sTypingMethod, string sGeneName)
        {
            DateTime Start = DateTime.Now;
            string sAlleleList = string.Empty;
            string sARSAlleleList = string.Empty;
            string sFRAlleleList = string.Empty;
            string sFREIAlleleList = string.Empty;
            List<string> lstAllele = new List<string>();
            string jsonResponse = string.Empty;
            string frMismatches = string.Empty;
            string freiMismatches = string.Empty;
            string[] lstFRAlleleParts;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://10.201.20.114:8080/AutoTypingService/autoTyping/?masterid=" + sGroupID);
                //seq = arrInput[0];               
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = strJSON.Length;
                using (Stream webStream = request.GetRequestStream())
                using (StreamWriter requestWriter = new StreamWriter(webStream, System.Text.Encoding.ASCII))
                {
                    requestWriter.Write(strJSON);
                }

                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream())
                {
                    if (webStream != null)
                    {
                        using (StreamReader responseReader = new StreamReader(webStream))
                        {
                            jsonResponse = responseReader.ReadToEnd();
                        }
                    }
                }
                if (jsonResponse != string.Empty)
                {
                    bool IsNewAllele = false;

                    if (sTypingMethod == "PacBio-R" || sTypingMethod == "NGS-ARS")// || sTypingMethod == "LR")
                    {
                        int iMismatchstart = jsonResponse.IndexOf("\"exonmismatches\":") + ("\"exonmismatches\":").Length;
                        int iMismatchEnd = jsonResponse.IndexOf(",", iMismatchstart);
                        string Mismatch = jsonResponse.Substring(iMismatchstart, iMismatchEnd - iMismatchstart);
                        if (Mismatch != "0")
                            IsNewAllele = true;

                        int iStart = jsonResponse.IndexOf("\"arsalleles\":\"") + ("\"arsalleles\":\"").Length;
                        int iEnd = jsonResponse.IndexOf("\",\"", iStart);
                        string ARSAllele = jsonResponse.Substring(iStart, iEnd - iStart);
                        lstAllele = ARSAllele.Replace(sGeneName + "_", sGeneName + "*").Replace("_", ":").Split(',').ToList();

                        if (sTypingMethod == "LR")
                            if (lstAllele != null && lstAllele.Count > 0)
                            {
                                lstAllele = lstAllele.Where(a => a.Contains(sGeneName + "*")).ToList();
                                lstAllele.Sort();
                                if (IsNewAllele == true)
                                {
                                    sARSAlleleList = lstAllele[0];
                                    sARSAlleleList = sARSAlleleList.Substring(0, sARSAlleleList.IndexOf(":") + 1) + "XX";
                                }
                                else
                                {
                                    sARSAlleleList = string.Join(",", lstAllele.ToArray());
                                }
                            }
                    }
                    if (sTypingMethod == "PacBio-FR" || sTypingMethod == "NGS-FR" || sTypingMethod == "LR")
                    {
                        lstAllele = new List<string>();
                        int iMismatchstart = jsonResponse.IndexOf("\"exonmismatches\":") + ("\"exonmismatches\":").Length;
                        int iMismatchEnd = jsonResponse.IndexOf(",", iMismatchstart);
                        string Mismatch = jsonResponse.Substring(iMismatchstart, iMismatchEnd - iMismatchstart);
                        if (Mismatch != "0")
                            IsNewAllele = true;
                        int iStart = jsonResponse.IndexOf("\"fralleles\":\"") + ("\"fralleles\":\"").Length;
                        int iEnd = jsonResponse.IndexOf("\",\"", iStart);
                        string FRAllele = jsonResponse.Substring(iStart, iEnd - iStart);
                        lstAllele = FRAllele.Replace(sGeneName + "_", sGeneName + "*").Replace("_", ":").Split(',').ToList();

                        if (sTypingMethod == "LR")
                        {
                            if (lstAllele != null && lstAllele.Count > 0)
                            {
                                lstAllele = lstAllele.Where(a => a.Contains(sGeneName + "*")).ToList();
                                lstAllele.Sort();
                                if (IsNewAllele == true)
                                {
                                    sFRAlleleList = lstAllele[0];
                                    int colonOccurances = sFRAlleleList.Length - sFRAlleleList.Replace(":", string.Empty).Length;
                                    if (colonOccurances == 1)
                                        sFRAlleleList = sFRAlleleList.Substring(0, sFRAlleleList.IndexOf(":") + 1) + "XX";
                                    else if (colonOccurances == 2)
                                        sFRAlleleList = sFRAlleleList.Substring(0, sFRAlleleList.LastIndexOf(":") + 1) + "XX";
                                    else if (colonOccurances > 2)
                                    {
                                        lstFRAlleleParts = sFRAlleleList.Split(':');
                                        sFRAlleleList = lstFRAlleleParts[0] + ":" + lstFRAlleleParts[1] + ":XX";
                                    }
                                }
                                else
                                {
                                    sFRAlleleList = string.Join(",", lstAllele.ToArray());
                                }

                                frMismatches = Mismatch;
                            }
                        }
                    }
                    if (sTypingMethod == "PacBio-FREI" || sTypingMethod == "NGS-FREI" || sTypingMethod == "LR")
                    {
                        lstAllele = new List<string>();
                        int iMismatchstart = jsonResponse.IndexOf("\"mismatches\":") + ("\"mismatches\":").Length;
                        int iMismatchEnd = jsonResponse.IndexOf(",", iMismatchstart);
                        string Mismatch = jsonResponse.Substring(iMismatchstart, iMismatchEnd - iMismatchstart);
                        if (Mismatch != "0")
                            IsNewAllele = true;

                        int iStart = jsonResponse.IndexOf("\"frexInAlleles\":\"") + ("\"frexInAlleles\":\"").Length;
                        int iEnd = jsonResponse.IndexOf("\",\"", iStart);
                        string FREIAllele = jsonResponse.Substring(iStart, iEnd - iStart);
                        lstAllele = FREIAllele.Replace(sGeneName + "_", sGeneName + "*").Replace("_", ":").Split(',').ToList();

                        if (sTypingMethod == "LR")
                        {
                            if (lstAllele != null && lstAllele.Count > 0)
                            {
                                lstAllele = lstAllele.Where(a => a.Contains(sGeneName + "*")).ToList();
                                lstAllele.Sort();
                                if (IsNewAllele == true)
                                {
                                    sFREIAlleleList = lstAllele[0];
                                    int colonOccurances = sFREIAlleleList.Length - sFREIAlleleList.Replace(":", string.Empty).Length;
                                    if (colonOccurances == 1)
                                        sFREIAlleleList = sFREIAlleleList.Substring(0, sFREIAlleleList.IndexOf(":") + 1) + "XX";
                                    if (colonOccurances > 1)
                                        sFREIAlleleList = sFREIAlleleList.Substring(0, sFREIAlleleList.LastIndexOf(":") + 1) + "XX";
                                }
                                else
                                {
                                    sFREIAlleleList = string.Join(",", lstAllele.ToArray());
                                }

                                freiMismatches = Mismatch;
                            }
                        }
                    }
                    if (sTypingMethod == "LR")
                        sAlleleList = sFRAlleleList + "!" + sFREIAlleleList + "$" + frMismatches + "!" + freiMismatches;
                    else
                        if (lstAllele != null && lstAllele.Count > 0)
                        {
                            lstAllele = lstAllele.Where(a => a.Contains(sGeneName + "*")).ToList();
                            lstAllele.Sort();
                            if (IsNewAllele == true)
                            {
                                sAlleleList = lstAllele[0];
                                sAlleleList = sAlleleList.Substring(0, sAlleleList.IndexOf(":") + 1) + "XX";
                            }
                            else
                            {
                                sAlleleList = string.Join(",", lstAllele.ToArray());
                            }
                        }
                }
                else
                {
                    // return "Error : No Data from Blast";
                    return "";
                }
            }
            catch (WebException webex)
            {
                //return "Error : Exception in Pacbio Blast Service;" + webex.Message;
                return "";
            }
            catch (Exception ex)
            {
                //return "Error : " + ex.Message;
                return "";
            }

            return sAlleleList;
        }
        #endregion

    }


}
