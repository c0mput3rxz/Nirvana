using UnitTests.Utilities;
using VariantAnnotation.FileHandling;
using Xunit;

namespace UnitTests.VcfParsing
{
    [Collection("Chromosome 1 collection")]
    public sealed class ParseVcfLineTests
    {
        [Fact]
        public void BlankRefName()
        {
            const string vcfLine = "1	1	.	N	.	.	LowGQX	END=10004;BLOCKAVG_min30p3a	GT:GQX:DP:DPF	.:.:0:0";
            var variant = VcfUtilities.GetVariantFeature(vcfLine);
            Assert.True(variant.IsReference);
        }

        [Fact]
        public void GenotypeParsing()
        {
            const string vcfLine = "chr1	24538137	.	C	.	.	PASS	.	GT:GQX:DP:DPF	0/0:99:34:2";
            var variant = VcfUtilities.GetVariantFeature(vcfLine);
            Assert.True(variant.IsReference);
        }

        [Fact]
        public void BreakEndBegin()
        {
            // NIR-1234
            const string vcfLine = "1	28722335	MantaBND:4051:0:2:0:0:0:0	T	[3:115024109[T	.	PASS	SVTYPE=BND;MATEID=MantaBND:4051:0:2:0:0:0:1;IMPRECISE;CIPOS=-209,210;SOMATIC;SOMATICSCORE=42;BND_DEPTH=23;MATE_BND_DEPTH=24     PR      25,0    71,10";
            var variant = VcfUtilities.GetVariantFeature(vcfLine);

            Assert.True(variant.IsStructuralVariant);
            Assert.Equal(28722335, variant.AlternateAlleles[0].ReferenceBegin);
        }

        [Fact]
        public void ParseStrandBias()
        {
            // NIR-1235
            const string vcfLine =
                "chrX	2699246	rs148553620	C	A	250.95	VQSRTrancheSNP99.00to99.90	AC=2;AF=0.250;AN=8;BaseQRankSum=1.719;DB;DP=106;Dels=0.00;FS=20.202;HaplotypeScore=0.0000;MLEAC=2;MLEAF=0.250;MQ=43.50;MQ0=52;MQRankSum=2.955;QD=4.73;ReadPosRankSum=1.024;SB=-1.368e+02;VQSLOD=-0.3503;culprit=MQ;PLF  GT:AD:DP:GQ:PL:AA   0:10,6:16:9:0,9,118:P1,.	   0|1:12,11:23:27:115,0,27:M1,M2   0|0:37,0:37:18:0,18,236:M1,P1   1|0:13,17:30:59:177,0,59:M2,P1";
            var variant = VcfUtilities.GetVariantFeature(vcfLine);

            Assert.Equal(-136.8, variant.StrandBias);
        }

        [Fact]
        public void SvBeginPosition()
        {
            const string vcfLine = "1	964001	.	A	<DEL>	79	PASS	END=964423;SVTYPE=DEL;SVLEN=-422;IMPRECISE;CIPOS=-170,170;CIEND=-175,175;CSQT=1|AGRN|ENST00000379370|   GT:GQ:PR	   0/1:79:34,23";
            var variant = VcfUtilities.GetVariantFeature(vcfLine);
            Assert.Equal(964002, variant.AlternateAlleles[0].ReferenceBegin);
        }

        [Fact]
        public void RepeatExpansion()
        {
            const string vcfLine = "chrX	146993568	FMR1	G	<REPEAT:EXPANSION>	1.0	NoSuppReads	REPEAT_COUNT1=30,33";
            var variant = VcfUtilities.GetVariantFeature(vcfLine);
            const int expectedReferenceBegin = 146993569;
            Assert.Equal(expectedReferenceBegin, variant.AlternateAlleles[0].ReferenceBegin);
        }

        [Fact]
        public void ParseSv()
        {
            const string vcfLine = "1	1594584	MantaDEL:164:0:1:1:0:0	C	<DEL>	.	MGE10kb	END=1660503;SVTYPE=DEL;SVLEN=-65919;IMPRECISE;CIPOS=-285,285;CIEND=-205,205;SOMATIC;SOMATICSCORE=36;ColocalizedCanvas PR 42,0 226,9";
            var variant = VcfUtilities.GetVariantFeature(vcfLine);

            Assert.Equal(1594584, variant.VcfReferenceBegin);
            Assert.Equal(1594585, variant.AlternateAlleles[0].ReferenceBegin);
            Assert.Equal(1660503, variant.AlternateAlleles[0].ReferenceEnd);
            Assert.Equal(new[] { "-285", "285" }, variant.CiPos);
            Assert.Equal(new[] { "-205", "205" }, variant.CiEnd);
        }

        [Fact]
        public void DeletionWithoutSymbolicAllele()
        {
            var annotatedVariant = DataUtilities.GetVariant("ENST00000257290_chr4_Ensembl84.ndb",
                "1	823830	.	AGAGAAGGAGAGAAGGAAGGAAGGAGGGAGAGAAAGGGAAAGAAGGAAAGAAACAAGGAAGCAGGGAGGGAGAGAAAGAGGAAGGGAG	A	495	MaxDepth;MaxMQ0Frac	END=823917;SVTYPE=DEL;SVLEN=-87;CIGAR=1M87D;ColocalizedCanvas	GT:GQ:PR:SR	0/1:495:80,0:93,25");

            var observedJsonLine = JsonUtilities.GetFirstAlleleJson(annotatedVariant);

            const string expectedJsonLine = "{\"altAllele\":\"-\",\"refAllele\":\"GAGAAGGAGAGAAGGAAGGAAGGAGGGAGAGAAAGGGAAAGAAGGAAAGAAACAAGGAAGCAGGGAGGGAGAGAAAGAGGAAGGGAG\",\"begin\":823831,\"chromosome\":\"1\",\"end\":823917,\"variantType\":\"deletion\",\"vid\":\"1:823831:823917\"}";
            Assert.Equal(expectedJsonLine, observedJsonLine);
        }

        /// <summary>
        /// The vcf info field may contain fields like AA, GMAF, CSQ*, etc. fields that need to be removed.
        /// </summary>
        [Fact]
        public void StripUnwantedVcfInfoFields()
        {
            const string vcfLine =
                "chr4	55141055	rs1873778	A	G	1000	PASS	SNVSB=-9.6;SNVHPOL=3;RefMinor;AA=G;GMAF=G|0.9577;AF1000G=0.957668;EVS=0.9589|89|6503;phyloP=-5.707;cosmic=COSM1430082;CSQT=1|PDGFRA|ENST00000257290|synonymous_variant;CSQR=1|ENSR00001241308|regulatory_region_variant	GT      0/1";

            var variant = VcfUtilities.GetVariantFeature(vcfLine);
            Assert.Equal("SNVSB=-9.6;SNVHPOL=3", variant.VcfColumns[VcfCommon.InfoIndex]);
        }

        [Fact]
        public void CiPosOfMantaDel()
        {
            // NIR-1305
            const string vcfLine = "1	2461646	MantaDEL:358:0:1:0:2:0	GCGAGACGAGGCGAGCGGGCGCGGGCAAGGCCAAGCGCGTCCCGGGCTGGCGCGGACACCGGCCC	G	.	MinSomaticScore	END=2461710;SVTYPE=DEL;SVLEN=-64;CIGAR=1M64D;CIPOS=-2,2;HOMLEN=2;HOMSEQ=CG;SOMATIC;SOMATICSCORE=22;ColocalizedCanvas	PR:SR	5,0:18,0	2,0:13,19";

            var variant = VcfUtilities.GetVariantFeature(vcfLine);

            Assert.Equal(2461646, variant.VcfReferenceBegin);
            Assert.Equal(2461647, variant.AlternateAlleles[0].ReferenceBegin);
            Assert.Null(variant.CiEnd);
            Assert.Equal(new[] { "-2", "2" }, variant.CiPos);
        }

		[Fact]
		public void CanvasCopyNumberShouldNotReportInPosition()
		{
			const string vcfLine = "1	723707	Canvas:GAIN:1:723708:2581225	N	<CNV>	41	PASS	SVTYPE=CNV;END=2581225	RC:BC:CN:MCC	.	129:3123:3:2";
			var variant = VcfUtilities.GetVariantFeature(vcfLine);
			Assert.Null(variant.CopyNumber);
		}

	    [Fact]
	    public void ParseStrelkaJointSomaticNormalQuality()
	    {
			const string vcfLine = "9\t46530\t.\tC\tCG\t.\tQSI_ref\tSOMATIC;QSI=2;TQSI=1;NT=het;QSI_NT=2;TQSI_NT=1;SGT=ref->ref;RU=G;RC=0;IC=1;IHP=2\tDP:DP2:TAR:TIR:TOR:DP50:FDP50:SUBDP50\t13:13:10,12:3,7:0,2:14.49:0.00:0.00\t72:72:67,67:0,1:5,6:75.31:0.38:0.00";
			var variant = VcfUtilities.GetVariantFeature(vcfLine);
			Assert.NotNull(variant.JointSomaticNormalQuality);
			Assert.Equal(2, variant.JointSomaticNormalQuality.Value);
		}

	    [Theory]
		[InlineData("chr1	55141050	.	A	G	1000	PASS	.	.", false)]
		[InlineData("chr4	55141055	.	A	G	1000	PASS	.	.", false)]
		[InlineData("chr4	55141055	.	A	G,T	1000	PASS	.	.", true)]
		[InlineData("chr4	55141055	.	C	G,T	1000	PASS	.	.", false)]
		[InlineData("chr4	55141050	.	A	G,T	1000	PASS	.	.", false)]
		[InlineData("chr4	55141055	.	A	T,G	1000	PASS	.	.", false)]
		public void VariantFeatureEquals(string vcfLine, bool isEqual)
	    {
		    var variant1 = VcfUtilities.GetVariantFeature("chr4	55141055	rs1873778	A	G,T	1000	PASS	.	.");
			var variant2 = VcfUtilities.GetVariantFeature(vcfLine);

            Assert.Equal(isEqual,variant1.Equals(variant2));
			Assert.Equal(true,variant1.Equals(variant1));
			Assert.Equal(false,variant1.Equals(null));
		}
	}
}
