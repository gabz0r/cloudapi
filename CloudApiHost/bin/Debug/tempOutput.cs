using CloudApiLib.Documents;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudApiLib 
{ 
    public class CtrlBusinessPartner : CADocument<CtrlBusinessPartner>
    {
		public String BusinessPartnerId { get; set; }
		public String Name1 { get; set; }
		public String Name2 { get; set; }
		public String Name3 { get; set; }
		public String Name4 { get; set; }
		public CtrlCountry Country { get; set; }
		public String State { get; set; }
		public String CityCode { get; set; }
		public String City { get; set; }
		public String Street { get; set; }
		public String HouseNumber { get; set; }


    }
public class CtrlArticle : CADocument<CtrlArticle>
    {
		public String IndexSearchString { get; set; }
		public String ArticleId { get; set; }
		public String Description1 { get; set; }
		public String Description2 { get; set; }
		public String Description3 { get; set; }
		public String Description4 { get; set; }
		public List<String> Suppliers { get; set; }
		public Decimal SellingPrice { get; set; }
		public Decimal LastPurchasePrice { get; set; }
		public CtrlBusinessPartner LastSupplier { get; set; }
		public Decimal CheapestPurchasePrice { get; set; }
		public CtrlBusinessPartner CheapestSupplier { get; set; }
		public DateTime CheapestPurchaseDate { get; set; }


    }
public class CtrlCountry : CADocument<CtrlCountry>
    {
		public String Code { get; set; }
		public String FullName { get; set; }


    }
public class CtrlDocumentPropName : CADocument<CtrlDocumentPropName>
    {
		public String CtrlDocumentName { get; set; }
		public Dictionary<String,String> PropertyNameLabelMapping { get; set; }


    }
public class CtrlException : CADocument<CtrlException>
    {
		public String Name { get; set; }
		public String StackTrace { get; set; }
		public String Message { get; set; }


    }
public class CtrlMainMenuItem : CADocument<CtrlMainMenuItem>
    {
		public String MenuNodeId { get; set; }
		public String Title { get; set; }
		public String Description { get; set; }
		public String AssociatedModule { get; set; }

		public static async Task AddItemAsFavourite(String iUsername, String iNodeId) 
		{
        var req = (HttpWebRequest) WebRequest.Create("http://localhost:9091/cloudapi/");
        req.ContentType = "cloudapi/body-encoded";
        req.Method = "POST";

        var data = Encoding.UTF8.GetBytes("OPCODE=CLOUD_API_METHOD&TYPE=CtrlBusinessLib.CtrlMainMenuItem&METHOD=AddItemAsFavourite#iUsername=" + iUsername + "&iNodeId=" + iNodeId + "");
        var stream = req.GetRequestStream();

        await stream.WriteAsync(data, 0, data.Length);
        await stream.FlushAsync();
        stream.Close();
        using(var response = await req.GetResponseAsync())
        {
            using(var respStream = response.GetResponseStream())
            {
                var sr = new StreamReader(respStream);
                var retDt = sr.ReadToEnd();return;
            }
        }
		}

		public static async Task RemoveItemFromFavourites(String iUsername, String iNodeId) 
		{
        var req = (HttpWebRequest) WebRequest.Create("http://localhost:9091/cloudapi/");
        req.ContentType = "cloudapi/body-encoded";
        req.Method = "POST";

        var data = Encoding.UTF8.GetBytes("OPCODE=CLOUD_API_METHOD&TYPE=CtrlBusinessLib.CtrlMainMenuItem&METHOD=RemoveItemFromFavourites#iUsername=" + iUsername + "&iNodeId=" + iNodeId + "");
        var stream = req.GetRequestStream();

        await stream.WriteAsync(data, 0, data.Length);
        await stream.FlushAsync();
        stream.Close();
        using(var response = await req.GetResponseAsync())
        {
            using(var respStream = response.GetResponseStream())
            {
                var sr = new StreamReader(respStream);
                var retDt = sr.ReadToEnd();return;
            }
        }
		}


    }
public class CtrlUser : CADocument<CtrlUser>
    {
		public String Username { get; set; }
		public String Password { get; set; }
		public String FirstName { get; set; }
		public String LastName { get; set; }
		public List<String> MenuFavourites { get; set; }


    }

}