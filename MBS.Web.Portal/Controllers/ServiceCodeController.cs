using MBS.DataCache;
using MBS.Web.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;

namespace MBS.Web.Portal.Controllers
{
    [Authorize(Roles= "Member,Administrator")]
    public class ServiceCodeController : Controller
    {
        private readonly bool _isAdmin = false;

        public ServiceCodeController()
        {
        }

        #region JSON Service - Code List Services

        public JsonResult GetFeeCode()
        {
            try
            {
                return Json(StaticCodeList.MyFeeCodeList.Concat(StaticCodeList.MyWCBFeeCodeList).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value).ToList(), JsonRequestBehavior.AllowGet);
            }
            catch
            {
            }

            return Json(Enumerable.Empty<KeyValuePair<string, StaticCodeList.FeeCodeModel>>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUnitCodeFee(string prefix, int claimType)
        {
            try
            {
                prefix = prefix.ToUpper();

                IEnumerable<KeyValuePair<string, StaticCodeList.FeeCodeModel>> combinedList;

                if (claimType == 0) //MSB
                {
                    combinedList = StaticCodeList.MyFeeCodeList;
                }
                else
                {
                    combinedList = StaticCodeList.MyFeeCodeList.Concat(StaticCodeList.MyWCBFeeCodeList);
                }

                var result = combinedList.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            .Select(x => new SimpleFeeModel()
                            {
                                key = x.Key,
                                label = x.Key.Contains(" - WCB") ? x.Key : x.Key + " - " + x.Value.Description,
                                requiredUnitTime = x.Value.RequiredStartTime == "Y",
                                requiredReferDoc = x.Value.RequiredReferringDoc == "Y",
                                value = x.Value.FeeAmount,
                                feeDeterminant = x.Value.FeeDeterminant
                            })
                            .OrderBy(x => x.orderKey)
                            .Take(30)
                            .ToList();
                
                return Json(result, JsonRequestBehavior.AllowGet);                
            }
            catch
            {                
            }

            return Json(Enumerable.Empty<SimpleFeeModel>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUnitCodeList(string unitcodelist)
        {
            try
            {
                var codesNeedToFetch = unitcodelist.Trim(',').Split(',').Distinct().Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
                var result = new List<SimpleFeeModel>();

                foreach (var unitcode in codesNeedToFetch)
                {
                    StaticCodeList.FeeCodeModel feeModel = null;
                    if (StaticCodeList.MyFeeCodeList.ContainsKey(unitcode))
                    {
                        feeModel = StaticCodeList.MyFeeCodeList[unitcode];

                        result.Add(new SimpleFeeModel()
                        {
                            key = unitcode,
                            label = unitcode + " - " + feeModel.Description,
                            requiredUnitTime = feeModel.RequiredStartTime == "Y",
                            requiredReferDoc = feeModel.RequiredReferringDoc == "Y",
                            value = feeModel.FeeAmount,
                            feeDeterminant = feeModel.FeeDeterminant
                        });
                    }

                    var wcbFeeCode = unitcode + " - WCB";
                    if (StaticCodeList.MyWCBFeeCodeList.ContainsKey(wcbFeeCode))
                    {
                        feeModel = StaticCodeList.MyWCBFeeCodeList[wcbFeeCode];

                        result.Add(new SimpleFeeModel()
                        {
                            key = wcbFeeCode,
                            label = wcbFeeCode,
                            requiredUnitTime = false,
                            requiredReferDoc = false,
                            value = feeModel.FeeAmount,
                            feeDeterminant = string.Empty
                        });
                    }
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch
            {
            }

            return Json(Enumerable.Empty<SimpleFeeModel>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSpecialFeeCode()
        {
            try
            {
                return Json(StaticCodeList.MySpecialCodeList.ToList(), JsonRequestBehavior.AllowGet);
            }
            catch
            {
            }

            return Json(Enumerable.Empty<KeyValuePair<string, StaticCodeList.FeeCodeModel>>(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetICDCodeList(string prefix)
        {
            var result = StaticCodeList.MyICDList.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || x.Value.ToUpper().Contains(prefix.ToUpper())).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetICDCodeDescription(string diagcode)
        {
            var message = string.Empty;
            if (StaticCodeList.MyICDList.ContainsKey(diagcode))
            {
                message = StaticCodeList.MyICDList[diagcode];
            }

            return Json(message, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetICDCodeDescriptionList(string diagcodelist)
        {
            var codesNeedToFetch = diagcodelist.TrimEnd(',').Split(',').Distinct().Select(x => x.ToUpper());
            var result = new List<ICD>();

            foreach (var diagcode in codesNeedToFetch)
            {
                var message = string.Empty;
                if (StaticCodeList.MyICDList.ContainsKey(diagcode))
                {
                    result.Add(new ICD(diagcode, StaticCodeList.MyICDList[diagcode]));
                }
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRefDocCodeList(string prefix)
        {
            var result = StaticCodeList.MyRefDocList.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || x.Value.ToUpper().Contains(prefix.ToUpper())).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRefDocNameList(string prefix)
        {
            var result = StaticCodeList.MyRefDocList.Where(x => x.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || x.Value.ToUpper().Contains(prefix.ToUpper())).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private string GetRefDocName(string code)
        {
            var result = string.Empty;
            if (StaticCodeList.MyRefDocList.ContainsKey(code))
            {
                result = StaticCodeList.MyRefDocList[code];

                var endIndex = result.LastIndexOf(" - ");
                result = result.Substring(0, endIndex);
            }

            return result;
        }
        
        public ActionResult GetExplainCode(string explainCode)
        {
            var result = string.Empty;

            if (!string.IsNullOrEmpty(explainCode))
            {
                explainCode = explainCode.ToUpper();
                if (StaticCodeList.MyExplainCodeList.ContainsKey(explainCode))
                {
                    result = StaticCodeList.MyExplainCodeList[explainCode];
                }
            }

            return Content(result);
        }

        #endregion
    }
    
}