using System.Text.Json;
using Ardalis.SmartEnum;
using Goatrello.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Goatrello.Services;


/// <summary>
/// Extension methods to Report Filter and Operation Data Models
/// Provides generic type data extraction from stored json string
/// Provides quick method to apply filters and get operation results
/// </summary>
public static class ReportingModelExtensions
{
        public static T GetValue<T>(this ReportFilter filter, string name)
        {
            try
            {
                var values = JObject.Parse(filter.Values);
                return values[name].ToObject<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public static void SetValue(this ReportFilter filter, string name, object value)
        {
            try
            {
                string currentValues = filter.Values ?? "{}";
                var values = JObject.Parse(currentValues);
                values[name] = JToken.FromObject(value);
                filter.Values = values.ToString(Formatting.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public static IQueryable<Card> Apply(this ReportFilter filter, IQueryable<Card> cards) =>
            filter.Type.GetFiltered(cards, filter);
        
        public static T GetValue<T>(this ReportOperation operation, string name)
        {
            try
            {
                var values = JObject.Parse(operation.Values);
                return values[name].ToObject<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public static void SetValue(this ReportOperation operation, string name, object value)
        {
            try
            {
                var values = JObject.Parse(operation.Values);
                values[name] = JToken.FromObject(value);
                operation.Values = values.ToString(Formatting.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public static string Result(this ReportOperation operation, IQueryable<Card> cards) =>
            operation.Type.GetResult(cards, operation);
}

public enum InputType
{
    Bool,
    String,
    BoardId,
    ListingId,
    Date
}

/// <summary>
/// Contains all of the logic and descriptions for the different filter types
/// Filters are static enum classes. Use the apply function to filter a query 
/// </summary>
public abstract class ReportFilterType : SmartEnum<ReportFilterType, string>
{
    //register available filter types
    public static readonly ReportFilterType Board = new FilterByBoard();
    public static readonly ReportFilterType Listing = new FilterByListing();
    public static readonly ReportFilterType Archived = new FilterByArchived();
    
    
    //Smart enum constructor and Interface
    private ReportFilterType(string name, string value) : base(name, value)  { }
    public abstract string GetDescription();
    public abstract List<KeyValuePair<string, InputType>> Values();
    public abstract IQueryable<Card> GetFiltered(IQueryable<Card> cards, ReportFilter filter);

    private sealed class FilterByBoard : ReportFilterType
    {
        public FilterByBoard() : base("By Board", "ByBoard") { }
        public override string GetDescription() => "Only show cards that exist on a specific board";
        public override List<KeyValuePair<string, InputType>> Values() => new List<KeyValuePair<string, InputType>>
        {
            new KeyValuePair<string, InputType>("BoardId", InputType.BoardId)
        };
        public override IQueryable<Card> GetFiltered(IQueryable<Card> cards, ReportFilter filter) =>
            cards
                .Include(c => c.Listing)
                .Where(c => c.Listing.BoardId == filter.GetValue<int>("BoardId"))
                .AsQueryable();
    }
    
    private sealed class FilterByListing : ReportFilterType
    {
        public FilterByListing() : base("By Listing", "ByListing") { }
        public override string GetDescription() => "Only show cards that exist on a specific listing";
        public override List<KeyValuePair<string, InputType>> Values() => new List<KeyValuePair<string, InputType>>
        {
            new KeyValuePair<string, InputType>("ListingId", InputType.ListingId)
        };
        public override IQueryable<Card> GetFiltered(IQueryable<Card> cards, ReportFilter filter) =>
            cards
                .Include(c => c.Listing)
                .Where(c => c.Listing.BoardId == filter.GetValue<int>("ListingId"))
                .AsQueryable();
    }
    
    private sealed class FilterByArchived : ReportFilterType
    {
        public FilterByArchived() : base("By Archived Status", "ByArchive") { }
        public override string GetDescription() => "Select cards based on if they are archived or not";
        public override List<KeyValuePair<string, InputType>> Values() => new List<KeyValuePair<string, InputType>>
        {
            new KeyValuePair<string, InputType>("IsArchived", InputType.Bool)
        };
        public override IQueryable<Card> GetFiltered(IQueryable<Card> cards, ReportFilter filter) =>
            cards
                .Where(c => c.IsArchived == filter.GetValue<bool>("IsArchived"))
                .AsQueryable();
    }
}

/// <summary>
/// Contains all of the logic and descriptions for the different operation types
/// Operations are static enum classes. Use the GetResult function to filter a query 
/// </summary>
public abstract class ReportOperationType : SmartEnum<ReportOperationType, string>
{
    //register available filter types
    public static readonly ReportOperationType Count = new OperationCount();
    public static readonly ReportOperationType StaticValue = new OperationStaticValue();
    public static readonly ReportOperationType SumCustomField = new OperationCustomFieldSum();
    public static readonly ReportOperationType AverageCustomField = new OperationCustomFieldAverage();
    public static readonly ReportOperationType MaxCustomField = new OperationCustomFieldMax();
    public static readonly ReportOperationType MinCustomField = new OperationCustomFieldMin();



    //Smart enum constructor and Interface
    private ReportOperationType(string name, string value) : base(name, value) { }
    public abstract string GetDescription();
    public abstract List<KeyValuePair<string, string>> Values();
    public abstract string GetResult(IQueryable<Card> cards, ReportOperation operation);

    private sealed class OperationCount : ReportOperationType
    {
        public OperationCount() : base("Count Number of Cards", "Count") { }
        public override string GetDescription() => "Reports the total number of cards after the filters are applied";
        public override List<KeyValuePair<string, string>> Values() => new List<KeyValuePair<string, string>>();
        public override string GetResult(IQueryable<Card> cards, ReportOperation operation) =>
            cards.Count().ToString();
    }
    
    private sealed class OperationStaticValue : ReportOperationType
    {
        public OperationStaticValue() : base("Display a static message", "StaticMessage") { }
        public override string GetDescription() => "Use this to display a static message everytime the report is run";
        public override List<KeyValuePair<string, string>> Values() => new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("StaticMessage", "text")
        };
        public override string GetResult(IQueryable<Card> cards, ReportOperation operation) =>
            operation.GetValue<string>("StaticMessage");
    }
    
    private sealed class OperationCustomFieldSum : ReportOperationType
    {
        public OperationCustomFieldSum() : base("Sum of a custom field", "SumCustomField") { }
        public override string GetDescription() => "Get the sum of the values for a custom field for the selected cards";
        public override List<KeyValuePair<string, string>> Values() => new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("CustomFieldId", "CustomField")
        };
        public override string GetResult(IQueryable<Card> cards, ReportOperation operation) =>
            cards.Include(c => c.CustomFieldDatas)
                .SelectMany<Card, CustomFieldData>(c => c.CustomFieldDatas)
                .Where(d => d.CustomField == operation.GetValue<CustomField>("CustomFieldId"))
                .Sum(d => decimal.Parse(d.Value)).ToString();
    }
    
    private sealed class OperationCustomFieldAverage : ReportOperationType
    {
        public OperationCustomFieldAverage() : base("Average of a Custom Field", "AvgCustomField") { }
        public override string GetDescription() => "Get the average of the values for a custom field for the selected cards";
        public override List<KeyValuePair<string, string>> Values() => new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("CustomFieldId", "CustomField")
        };
        public override string GetResult(IQueryable<Card> cards, ReportOperation operation) =>
            cards.Include(c => c.CustomFieldDatas)
                .SelectMany<Card, CustomFieldData>(c => c.CustomFieldDatas)
                .Where(d => d.CustomField == operation.GetValue<CustomField>("CustomFieldId"))
                .Average(d => decimal.Parse(d.Value)).ToString();
    }
    
    private sealed class OperationCustomFieldMax : ReportOperationType
    {
        public OperationCustomFieldMax() : base("Maximum Value of a Custom Field", "MaxCustomField") { }
        public override string GetDescription() => "Get the Maximum of the values for a custom field for the selected cards";
        public override List<KeyValuePair<string, string>> Values() => new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("CustomFieldId", "CustomField")
        };
        public override string GetResult(IQueryable<Card> cards, ReportOperation operation) =>
            cards.Include(c => c.CustomFieldDatas)
                .SelectMany<Card, CustomFieldData>(c => c.CustomFieldDatas)
                .Where(d => d.CustomField == operation.GetValue<CustomField>("CustomFieldId"))
                .Max(d => decimal.Parse(d.Value)).ToString();
    }
    
    private sealed class OperationCustomFieldMin : ReportOperationType
    {
        public OperationCustomFieldMin() : base("Minimum Value of a Custom Field", "MinCustomField") { }
        public override string GetDescription() => "Get the Minimum of the values for a custom field for the selected cards";
        public override List<KeyValuePair<string, string>> Values() => new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("CustomFieldId", "CustomField")
        };
        public override string GetResult(IQueryable<Card> cards, ReportOperation operation) =>
            cards.Include(c => c.CustomFieldDatas)
                .SelectMany<Card, CustomFieldData>(c => c.CustomFieldDatas)
                .Where(d => d.CustomField == operation.GetValue<CustomField>("CustomFieldId"))
                .Min(d => decimal.Parse(d.Value)).ToString();
    }
}
