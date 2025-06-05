using System;
using System.ComponentModel.DataAnnotations;
using MBS.DomainModel;
using MBS.Web.Portal.Constants;
using System.Collections.Generic;
using System.Web.Mvc;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace MBS.Web.Portal.Models
{
    public class SearchClaimResult
    {
        public int draw { get; set; }

        public int recordsTotal { get; set; }

        public int recordsFiltered { get; set; }

        public IEnumerable<SearchResultItem> data { get; set; }
    }

    public class SearchResultItem
    {
        public string UserName { get; set; }

        public string LastName { get; set; }

        public string FirstName { get; set; }

        public string HospitalNumber { get; set; }

        public string ServiceDateString { get; set; }

        public string ClaimNumber { get; set; }
        
        public string PaidOrRejected { get; set; }

        public string ServiceRecordId { get; set; }

        public string PmtAppDateString { get; set; }

        public string ClaimAmountString { get; set; }

        public string PaidAmountString { get; set; }

        public string VarianceString { get; set; }

        public string ColorRangeString { get; set; }

        public string ExplainCodesString { get; set; }

        public string SubmissionDateString { get; set; }

        public string CPSClaimNumber { get; set; }

        public string FeeCodesString { get; set; }

        public string ExplainCode { get; set; }

        public string ExplainCode2 { get; set; }

        public string ExplainCode3 { get; set; }

        public string RunCode { get; set; }

        public string DiagCode { get; set; }

        public string UnitCode { get; set; }

        public int UnitNumber { get; set; }

        public string Status { get; set; }

        public string DT_RowId { get; set; }
    }

    public class JQueryDataTableParam
    {
        public int draw { get; set; }

        public int start { get; set; }
        
        public int length { get; set; }

        public JQueryDataTableSearch search { get; set; }

        public JQueryDataTableColumn[] columns { get; set; }

        public JQueryDataTableOrder[] order { get; set; }

        public string SearchClaimNumber { get; set; }

        public string SearchLastName { get; set; }

        public string SearchFirstName { get; set; }

        public string SearchClinicName { get; set; }

        public string SearchHSN { get; set; }

        public int SearchClaimTypeList { get; set; }

        public string SearchServiceStartDateString { get; set; }
            
        public string SearchServiceEndDateString { get; set; }

        public string SearchSubmissionStartDateString { get; set; }

        public string SearchSubmissionEndDateString { get; set; }

        public string SearchExplainCode { get; set; }

        public string SearchUnitCode { get; set; }

        public string SearchDiagCode { get; set; }

        public string SearchCPSClaimNumber { get; set; }

        public bool SearchUnsubmitted { get; set; }

        public bool SearchSubmitted { get; set; }

        public bool SearchPending { get; set; }

        public bool SearchPaid { get; set; }

        public bool SearchRejected { get; set; }

        public bool SearchDeleted { get; set; }        
    }

    public class JQueryDataTableColumn
    {
        public string data { get; set; }

        public string name { get; set; }

        public bool searchable { get; set; }

        public bool orderable { get; set; }

        public JQueryDataTableSearch search { get; set; }
    }

    public class JQueryDataTableSearch
    {
        public string value { get; set; }

        public bool regex { get; set; }

    }

    public class JQueryDataTableOrder
    {
        public int column { get; set; }

        public string dir { get; set; }

    }
}