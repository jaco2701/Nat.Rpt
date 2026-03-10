using Applet.Nat.Rpt;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using IDAutomation.Windows.Forms.QRCodeBarcode;
using Nat.Rpt.Models;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Http;
using System.Web.UI;
using System.Xml;

namespace Nat.Rpt.Controllers
{
    public class GenPdfController : ApiController
    {
        public HttpResponseMessage Post([FromBody] PrintRequest vioRequest)
        {
            HttpResponseMessage lioResult = new HttpResponseMessage(HttpStatusCode.OK);
            PrintResponse lioResponse = new PrintResponse();
            ReportDocument lioReport = new ReportDocument();
            try
            {
                string vivstrPath = $"{System.Configuration.ConfigurationManager.AppSettings["templates"]}\\{vioRequest.ivstrTemplatePath}";
                string livstrfilename = Path.GetTempFileName() + ".pdf", livstrXml = Encoding.UTF8.GetString(Convert.FromBase64String(vioRequest.ivstrB64Document));
                XmlDocument lioDocument = new XmlDocument();
                lioDocument.LoadXml(livstrXml);
                //Conversion QR
                XmlNamespaceManager lioNsMngr = new XmlNamespaceManager(lioDocument.NameTable);
                lioNsMngr.AddNamespace("ns", "http://www.afip.com.ar/fe");
                XmlNode lioXmlDocumentNode = lioDocument.SelectSingleNode("//ns:DTE/ns:Autorizacion/ns:BarCodeFont", lioNsMngr);
                if (lioXmlDocumentNode != null)
                {
                    string livstrQR = lioXmlDocumentNode.InnerText;
                    QRCodeBarcode lioQRCodeBarcode = new QRCodeBarcode();
                    lioXmlDocumentNode.InnerText = lioQRCodeBarcode.FontEncode(livstrQR, true, QRCodeBarcode.EncodingModes.Byte, QRCodeBarcode.Versions.AUTO, QRCodeBarcode.ErrorCorrectionLevels.Q);
                }
                lioReport.Load(vivstrPath);
                DataSet lioData = new DataSet("NewDataSet");
                vivstrPath = $"{System.Configuration.ConfigurationManager.AppSettings["schemas"]}\\DTE_v1.0.xsd";
                lioData.ReadXmlSchema(vivstrPath);
                lioData.EnforceConstraints = false;
                lioData.ReadXml(new XmlNodeReader(lioDocument));
                lioReport.SetDataSource(lioData);
                SetParameters(lioReport.DataDefinition.ParameterFields.GetEnumerator(), lioData, lioReport, string.Empty, "AuthCode", "AuthDate", 50);
                foreach (ReportObject reportObject in (SCRCollection)lioReport.ReportDefinition.ReportObjects)
                {
                    if (reportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        ReportDocument reportDocument = lioReport.OpenSubreport(((SubreportObject)reportObject).SubreportName);
                        this.SetParameters(reportDocument.DataDefinition.ParameterFields.GetEnumerator(), lioData, lioReport, reportDocument.Name, "AuthCode", "AuthDate", 50);
                    }
                }
                //lioReport.Refresh();
                DiskFileDestinationOptions crDiskFileDestinationOptions = new DiskFileDestinationOptions();
                CrystalDecisions.Shared.ExportOptions crExportOptions = lioReport.ExportOptions;
                crDiskFileDestinationOptions.DiskFileName = livstrfilename;
                crExportOptions.DestinationOptions = crDiskFileDestinationOptions;
                crExportOptions.ExportDestinationType = ExportDestinationType.DiskFile;
                crExportOptions.ExportFormatType = ExportFormatType.PortableDocFormat;
                lioReport.Export();
                lioResponse.ivstrB64Pdf = Statics.Compress(File.ReadAllBytes(livstrfilename));
                File.Delete(livstrfilename);
                lioReport.Dispose();
                lioReport = null;
                lioResult.Content = new ObjectContent<PrintResponse>(lioResponse, new JsonMediaTypeFormatter());
                return lioResult;
            }
            catch (Exception lioEx)
            {
                lioReport.Dispose();
                lioReport = null;
                lioResult = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                lioResult.Content = new StringContent(lioEx.ToString());
                return lioResult;
            }
        }

        public string Get()
        {
            return "ok";
        }

        private Dictionary<string, string> SetParameters(IEnumerator prms, DataSet dtsDocument, ReportDocument docPreview, string subReport, string AuthCode, string AuthDate, short DocIdStatus)
        {
            Dictionary<string, string> lioret = new Dictionary<string, string>();
            string name1 = string.Empty;
            string name2 = string.Empty;
            string name3 = string.Empty;
            if (dtsDocument.Tables.Contains("Extensions"))
            {
                name1 = "NumericField";
                name2 = "StringField";
                name3 = "DateField";
            }
            else if (dtsDocument.Tables.Contains("Personalizados"))
            {
                name1 = "campoNumero";
                name2 = "campoString";
                name3 = "campoFecha";
            }
            while (prms.MoveNext())
            {
                ParameterFieldDefinition current = (ParameterFieldDefinition)prms.Current;
                ParameterValues currentValues = current.CurrentValues;
                ParameterDiscreteValue parameterDiscreteValue = new ParameterDiscreteValue();
                switch (current.ParameterValueKind)
                {
                    case ParameterValueKind.NumberParameter:
                        if (current.Name == nameof(DocIdStatus))
                        {
                            parameterDiscreteValue.Value = (object)DocIdStatus;
                            break;
                        }
                        if (current.Name == "CopyNo")
                        {
                            parameterDiscreteValue.Value = (object)1;
                            break;
                        }
                        DataRow[] dataRowArray1 = dtsDocument.Tables[name1].Select("name='" + current.Name + "'");
                        parameterDiscreteValue.Value = dataRowArray1.Length == 0 || dataRowArray1[0].IsNull(name1 + "_Text") ? (object)0 : dataRowArray1[0][name1 + "_Text"];
                        break;
                    case ParameterValueKind.DateParameter:
                        DataRow[] dataRowArray2 = dtsDocument.Tables[name3].Select("name='" + current.Name + "'");
                        parameterDiscreteValue.Value = dataRowArray2.Length == 0 || dataRowArray2[0].IsNull(name3 + "_Text") ? (object)DateTime.MinValue : dataRowArray2[0][name3 + "_Text"];
                        break;
                    case ParameterValueKind.StringParameter:
                        if (current.Name == nameof(AuthCode) && AuthCode != null)
                        {
                            parameterDiscreteValue.Value = (object)AuthCode;
                            break;
                        }
                        DataRow[] dataRowArray3 = dtsDocument.Tables[name2].Select("name='" + current.Name + "'");
                        parameterDiscreteValue.Value = dataRowArray3.Length == 0 || dataRowArray3[0].IsNull(name2 + "_Text") ? (object)string.Empty : dataRowArray3[0][name2 + "_Text"];
                        break;
                    case ParameterValueKind.DateTimeParameter:
                        if (current.Name == nameof(AuthDate) && AuthDate != null)
                        {
                            DateTime dateTime = Convert.ToDateTime(AuthDate);
                            parameterDiscreteValue.Value = (object)dateTime;
                            break;
                        }
                        if (AuthDate == null)
                        {
                            parameterDiscreteValue.Value = (object)DateTime.MinValue;
                            break;
                        }
                        break;
                }
                lioret.Add(current.Name, parameterDiscreteValue.Value.ToString());
                if (!current.Name.StartsWith("Pm-"))
                {
                    if (subReport == string.Empty)
                        docPreview.SetParameterValue(current.Name, parameterDiscreteValue.Value);
                    else
                        docPreview.SetParameterValue(current.Name, parameterDiscreteValue.Value, subReport);
                    currentValues.Add((ParameterValue)parameterDiscreteValue);
                    current.ApplyCurrentValues(currentValues);
                }

            }
            return lioret;
        }
    }
}
