﻿using System.Collections.Generic;

namespace VariantAnnotation.Interface
{
	public interface IAnnotationSource
	{
		/// <summary>
		/// Performs annotation on a variant and returns an annotated variant
		/// </summary>
		/// <param name="variant">a variant object</param>
		/// <returns>an annotated variant</returns>
		IAnnotatedVariant Annotate(IVariant variant);

		IEnumerable<IDataSourceVersion> GetDataSourceVersions();

		string GetGenomeAssembly();

	  List<IGeneAnnotation> GetGeneAnnotations();

	    void EnableReferenceNoCalls(bool limitReferenceNoCallsToTranscripts);

        void EnableMitochondrialAnnotation();

        string GetDataVersion();

        void FinalizeMetrics();
    }
}
