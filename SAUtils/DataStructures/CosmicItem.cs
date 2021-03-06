﻿using System;
using System.Collections.Generic;
using System.Text;
using VariantAnnotation.DataStructures.SupplementaryAnnotations;
using VariantAnnotation.FileHandling.JSON;
using VariantAnnotation.Interface;

namespace SAUtils.DataStructures
{
	public sealed class CosmicItem: SupplementaryDataItem,  IEquatable<CosmicItem>
	{
		#region members
		public string ID { get; }
	    private string Gene { get; }
	    private int? SampleCount { get; }


	    // ReSharper disable once UnusedAutoPropertyAccessor.Local
	    private string IsAlleleSpecific { get; set; }

		public HashSet<CosmicStudy> Studies { get; private set; }

		#endregion

	    public CosmicItem(
			string chromosome,
			int start,
			string id,
			string refAllele,
			string altAllele,
			string gene,
			HashSet<CosmicStudy> studies, int? sampleCount)
		{
			Chromosome        = chromosome;
			Start             = start;
			ID                = id;
			ReferenceAllele   = refAllele;
			AlternateAllele   = altAllele;
		    Gene              = gene;

			Studies = studies;
			SampleCount = sampleCount;

		}

		public sealed class CosmicStudy : IEquatable<CosmicStudy>,IJsonSerializer
		{
			#region members

			public string ID { get; }
			public string Histology { get; }
			public string PrimarySite { get; }
			#endregion

			public CosmicStudy(string studyId, string histology, string primarySite)
			{
				ID          = studyId;
				Histology   = histology;
				PrimarySite = primarySite;
			}

		    public bool Equals(CosmicStudy other)
			{
				return ID.Equals(other?.ID) &&
					   Histology.Equals(other?.Histology) &&
					   PrimarySite.Equals(other?.PrimarySite);
			}

		    public override int GetHashCode()
			{
				var hashCode= ID?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ (Histology?.GetHashCode() ?? 0);
				hashCode = (hashCode * 397) ^ (PrimarySite?.GetHashCode() ?? 0);
				return hashCode;
			}


			public void SerializeJson(StringBuilder sb)
			{
				var jsonObject = new JsonObject(sb);

				sb.Append(JsonObject.OpenBrace);
				if (!string.IsNullOrEmpty(ID)) jsonObject.AddStringValue("id", ID, false);
				jsonObject.AddStringValue("histology", Histology?.Replace('_', ' '));
				jsonObject.AddStringValue("primarySite", PrimarySite?.Replace('_', ' '));
				sb.Append(JsonObject.CloseBrace);
			}
		}



		public override SupplementaryInterval GetSupplementaryInterval(IChromosomeRenamer renamer)
		{
			throw new NotImplementedException();
		}


		public bool Equals(CosmicItem otherItem)
		{
			// If parameter is null return false.
			if (otherItem == null) return false;

			// Return true if the fields match:
			return string.Equals(Chromosome, otherItem.Chromosome) &&
			       Start == otherItem.Start &&
			       string.Equals(ID, otherItem.ID) &&
			       string.Equals(ReferenceAllele, otherItem.ReferenceAllele) &&
			       string.Equals(AlternateAllele, otherItem.AlternateAllele) &&
			       string.Equals(Gene, otherItem.Gene) ;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Chromosome?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ Start;
				hashCode = (hashCode * 397) ^ (ID?.GetHashCode() ?? 0);
				hashCode = (hashCode * 397) ^ (ReferenceAllele?.GetHashCode() ?? 0);
				hashCode = (hashCode * 397) ^ (AlternateAllele?.GetHashCode() ?? 0);
				hashCode = (hashCode * 397) ^ (Gene?.GetHashCode() ?? 0);
				
				return hashCode;
			}
		}




		public string GetJsonString()
		{
			var sb = new StringBuilder();

			var jsonObject = new JsonObject(sb);

			jsonObject.AddStringValue("id", ID);
			jsonObject.AddStringValue("isAlleleSpecific", IsAlleleSpecific, false);
			jsonObject.AddStringValue("refAllele", string.IsNullOrEmpty(ReferenceAllele)?"-":ReferenceAllele);
			jsonObject.AddStringValue("altAllele", SupplementaryAnnotationUtilities.ReverseSaReducedAllele(AlternateAllele));
			jsonObject.AddStringValue("gene", Gene);
			jsonObject.AddIntValue("sampleCount", SampleCount);
			jsonObject.AddObjectValues("studies", Studies);

			return sb.ToString();
		}

		public void MergeStudies(CosmicItem otherItem)
		{
			if (Studies == null)
				Studies = otherItem.Studies;
			else
			{
				foreach (var study in otherItem.Studies)
				{
					Studies.Add(study);
				}
			}
		}
	}
}
