﻿using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VariantAnnotation.DataStructures.SupplementaryAnnotations;
using VariantAnnotation.FileHandling.JSON;
using VariantAnnotation.Interface;

namespace SAUtils.DataStructures
{
    public sealed class CustomItem : SupplementaryDataItem, IJsonSerializer
    {
		public string Id { get; }
		public string AnnotationType { get; }
        private string IsAlleleSpecific { get; }
		public Dictionary<string, string> StringFields { get; }
        private Dictionary<string, double> NumberFields { get; }

		public List<string> BooleanFields { get; }


        public CustomItem(string chromosome, int start, string referenceAllele, string alternateAllele, string annotationType, string id, Dictionary<string, string> stringFields, Dictionary<string, double> numberFields,  List<string> boolFields, string isAlleleSpecific=null)
        {
            Chromosome       = chromosome;
            Start            = start;
            ReferenceAllele  = referenceAllele;
            AlternateAllele  = alternateAllele;
            AnnotationType   = annotationType;
            Id               = id;
            StringFields     = stringFields;
            NumberFields     = numberFields;
            BooleanFields    = boolFields;
            IsAlleleSpecific = isAlleleSpecific;
        }

        public override bool Equals(object other)
		{
			var otherItem = other as CustomItem;
			if (otherItem == null) return false;

			return Chromosome.Equals(otherItem.Chromosome)
				   && Start.Equals(otherItem.Start)
				   && ReferenceAllele.Equals(otherItem.ReferenceAllele)
				   && AlternateAllele.Equals(otherItem.AlternateAllele)
				   && AnnotationType.Equals(otherItem.AnnotationType);
		}

		public override int GetHashCode()
		{
            // ReSharper disable NonReadonlyMemberInGetHashCode
            var hashCode = Start.GetHashCode() ^ Chromosome.GetHashCode();
            hashCode = (hashCode * 397) ^ (AlternateAllele?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (AnnotationType?.GetHashCode() ?? 0);
            // ReSharper restore NonReadonlyMemberInGetHashCode

            return hashCode;
        }

        public string GetJsonString()
	    {
			var sb = new StringBuilder();
			var jsonObject = new JsonObject(sb);

			jsonObject.AddStringValue("id", Id);
			jsonObject.AddStringValue("altAllele", "N" == AlternateAllele ? null : AlternateAllele);
			jsonObject.AddStringValue("isAlleleSpecific", IsAlleleSpecific, false);

			if (StringFields != null)
				foreach (var stringField in StringFields)
				{
					jsonObject.AddStringValue(stringField.Key, stringField.Value);
				}

			if (NumberFields != null)
				foreach (var numFields in NumberFields)
				{
					jsonObject.AddStringValue(numFields.Key, numFields.Value.ToString(CultureInfo.InvariantCulture), false);
				}

			if (BooleanFields != null)
				foreach (var booleanField in BooleanFields)
				{
					jsonObject.AddBoolValue(booleanField, true, true, "true");
				}
			return sb.ToString();
	    }

		public void SerializeJson(StringBuilder sb)
		{
			var jsonObject = new JsonObject(sb);

			sb.Append(JsonObject.OpenBrace);
			jsonObject.AddStringValue("id", Id);
			jsonObject.AddStringValue("altAllele", "N" == AlternateAllele ? null : AlternateAllele);
			jsonObject.AddStringValue("isAlleleSpecific", IsAlleleSpecific, false);

			if (StringFields != null)
				foreach (var stringField in StringFields)
				{
					jsonObject.AddStringValue(stringField.Key, stringField.Value);
				}

			if (NumberFields != null)
				foreach (var numFields in NumberFields)
				{
					jsonObject.AddStringValue(numFields.Key, numFields.Value.ToString(CultureInfo.InvariantCulture),false);
				}

			if (BooleanFields != null)
				foreach (var booleanField in BooleanFields)
				{
					jsonObject.AddBoolValue(booleanField, true, true, "true");
				}
			sb.Append(JsonObject.CloseBrace);
		}
		public override SupplementaryInterval GetSupplementaryInterval(IChromosomeRenamer renamer)
		{
			throw new System.NotImplementedException();
		}

	}
}
