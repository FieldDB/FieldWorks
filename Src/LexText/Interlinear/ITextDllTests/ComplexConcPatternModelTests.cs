﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class ComplexConcPatternModelTests : MemoryOnlyBackendProviderTestBase
	{
		private IPartOfSpeech m_noun;
		private IPartOfSpeech m_verb;
		private IPartOfSpeech m_adj;
		private FDO.IText m_text;
		private ICmPossibility m_np;
		private IEqualityComparer<IParaFragment> m_fragmentComparer;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_fragmentComparer = new ParaFragmentEqualityComparer();

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					m_noun = MakePartOfSpeech("noun");
					m_verb = MakePartOfSpeech("verb");
					m_adj = MakePartOfSpeech("adj");

					m_np = Cache.LangProject.GetDefaultTextTagList().ReallyReallyAllPossibilities.Single(poss => poss.Abbreviation.BestAnalysisAlternative.Text == "Noun Phrase");

					ILexEntry ni = MakeEntry("ni-", m_verb, "1SgSubj");
					ILexEntry him = MakeEntry("him-", m_verb, "3SgObj");
					ILexEntry bili = MakeEntry("bili", m_verb, "to see");
					ILexEntry ra = MakeEntry("-ra", m_verb, "Pres");

					ILexEntry pus = MakeEntry("pus", m_adj, "green");

					ILexEntry yalo = MakeEntry("yalo", m_noun, "mat");
					ILexEntry la = MakeEntry("-la", m_noun, "1SgPoss");

					MakeWordform("nihimbilira", "I see", m_verb, ni, him, bili, ra);
					MakeWordform("pus", "green", m_adj, pus);
					MakeWordform("yalola", "my mat", m_noun, yalo, la);

					m_text = MakeText("nihimbilira pus, yalola.");

					var para = (IStTxtPara) m_text.ContentsOA.ParagraphsOS.First();
					MakeTag(m_text, m_np, para.SegmentsOS.First(), 1, para.SegmentsOS.First(), 3);
				});
		}

		private void MakeTag(FDO.IText text, ICmPossibility tag, ISegment beginSeg, int begin, ISegment endSeg, int end)
		{
			ITextTag ttag = Cache.ServiceLocator.GetInstance<ITextTagFactory>().Create();
			text.ContentsOA.TagsOC.Add(ttag);
			ttag.TagRA = tag;
			ttag.BeginSegmentRA = beginSeg;
			ttag.BeginAnalysisIndex = begin;
			ttag.EndSegmentRA = endSeg;
			ttag.EndAnalysisIndex = end;
		}

		private FDO.IText MakeText(string contents)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = Cache.TsStrFactory.MakeString(contents, Cache.DefaultVernWs);

			using (var pp = new ParagraphParser(Cache))
			{
				pp.Parse(para);
			}

			ISegment seg = para.SegmentsOS.First();
			for (int i = 0; i < seg.AnalysesRS.Count; i++)
			{
				IAnalysis analysis = seg.AnalysesRS[i];
				var wordform = analysis as IWfiWordform;
				if (wordform != null)
					seg.AnalysesRS[i] = wordform.AnalysesOC.First().MeaningsOC.First();
			}
			return text;
		}

		private IPartOfSpeech MakePartOfSpeech(string name)
		{
			var partOfSpeech = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(partOfSpeech);
			partOfSpeech.Name.AnalysisDefaultWritingSystem = MakeAnalysisString(name);
			partOfSpeech.Abbreviation.AnalysisDefaultWritingSystem = partOfSpeech.Name.AnalysisDefaultWritingSystem;
			return partOfSpeech;
		}

		private Guid GetSlotType(string form)
		{
			var slotType = MoMorphTypeTags.kguidMorphStem;
			if (form.StartsWith("-"))
				slotType = MoMorphTypeTags.kguidMorphSuffix;
			else if (form.EndsWith("-"))
				slotType = MoMorphTypeTags.kguidMorphPrefix;
			return slotType;
		}

		private ILexEntry MakeEntry(string lf, IPartOfSpeech pos, string gloss)
		{
			// The entry itself.
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			// Lexeme Form and MSA.

			Guid slotType = GetSlotType(lf);
			IMoForm form;
			IMoMorphSynAnalysis msa = GetMsaAndMoForm(entry, slotType, pos, out form);
			entry.LexemeFormOA = form;
			string trimmed = lf.Trim('-');
			form.Form.VernacularDefaultWritingSystem = MakeVernString(trimmed);
			form.Form.AnalysisDefaultWritingSystem = MakeAnalysisString(trimmed);
			form.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(slotType);
			// Bare bones of Sense
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = MakeAnalysisString(gloss);
			sense.MorphoSyntaxAnalysisRA = msa;
			return entry;
		}

		private IMoMorphSynAnalysis GetMsaAndMoForm(ILexEntry entry, Guid slotType, IPartOfSpeech pos, out IMoForm form)
		{
			IMoMorphSynAnalysis msa;
			if (slotType == MoMorphTypeTags.kguidMorphStem)
			{
				form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				var stemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				msa = stemMsa;
				entry.MorphoSyntaxAnalysesOC.Add(msa);
				stemMsa.PartOfSpeechRA = pos;
			}
			else
			{
				form = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				var affixMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
				msa = affixMsa;
				entry.MorphoSyntaxAnalysesOC.Add(msa);
				affixMsa.PartOfSpeechRA = pos;
			}
			return msa;
		}

		private void MakeWordform(string form, string gloss, IPartOfSpeech pos, params ILexEntry[] morphs)
		{
			var result = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			result.Form.VernacularDefaultWritingSystem = MakeVernString(form);

			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			result.AnalysesOC.Add(wa);
			wa.CategoryRA = pos;
			foreach (ILexEntry morph in morphs)
			{
				var bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
				wa.MorphBundlesOS.Add(bundle);
				bundle.MorphRA = morph.LexemeFormOA;
				bundle.MsaRA = morph.MorphoSyntaxAnalysesOC.First();
				bundle.SenseRA = morph.SensesOS.First();
			}

			var wg = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wg);
			wg.Form.AnalysisDefaultWritingSystem = MakeAnalysisString(gloss);
		}

		private ITsString MakeVernString(string content)
		{
			return Cache.TsStrFactory.MakeString(content, Cache.DefaultVernWs);
		}
		private ITsString MakeAnalysisString(string content)
		{
			return Cache.TsStrFactory.MakeString(content, Cache.DefaultAnalWs);
		}

		[Test]
		public void SimpleSearch()
		{
			var para = (IStTxtPara) m_text.ContentsOA.ParagraphsOS.First();
			ISegment seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcWordNode {Category = m_verb});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_noun});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcTagNode {Tag = m_np});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 12, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 12, 15, null), new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Form = MakeVernString("him")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Entry = MakeVernString("ra")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Gloss = MakeAnalysisString("1SgPoss")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_adj, NegateCategory = true});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_adj, NegateCategory = true, Form = MakeVernString("him"), Entry = MakeVernString("him"), Gloss = MakeAnalysisString("3SgObj")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_adj, NegateCategory = true, Form = MakeVernString("him"), Entry = MakeVernString("hin"), Gloss = MakeAnalysisString("3SgObj")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode());
			model.Root.Children.Add(new ComplexConcTagNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 23, null)}).Using(m_fragmentComparer));
		}

		[Test]
		public void WordBoundarySearch()
		{
			var para = (IStTxtPara) m_text.ContentsOA.ParagraphsOS.First();
			ISegment seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcMorphNode {Form = MakeVernString("bili")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Form = MakeVernString("bili")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Form = MakeVernString("bili")});
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_noun});
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_noun});
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_noun});
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_adj});
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_noun});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_adj});
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_noun});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 12, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_adj});
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_noun});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Minimum = 0, Maximum = -1});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null),
				new ParaFragment(seg, 12, 15, null), new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));
		}

		[Test]
		public void QuantifierSearch()
		{
			var para = (IStTxtPara) m_text.ContentsOA.ParagraphsOS.First();
			ISegment seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcWordNode {Minimum = 0, Maximum = -1});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_noun});
			model.Root.Children.Add(new ComplexConcWordNode {Minimum = 0, Maximum = -1});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode {Gloss = MakeAnalysisString("1SgSubj")});
			model.Root.Children.Add(new ComplexConcMorphNode {Minimum = 1, Maximum = -1});
			model.Root.Children.Add(new ComplexConcMorphNode {Form = MakeVernString("ra")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_verb});
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_adj, Minimum = 0, Maximum = 1});
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_noun});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_verb, Minimum = 1, Maximum = 3});
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Category = m_verb, Minimum = 1, Maximum = 4});
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null)}).Using(m_fragmentComparer));
		}

		[Test]
		public void AlternationSearch()
		{
			var para = (IStTxtPara) m_text.ContentsOA.ParagraphsOS.First();
			ISegment seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcWordNode {Category = m_noun});
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_verb});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_noun});
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_verb});
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcGroupNode {Children =
				{
					new ComplexConcWordNode {Category = m_verb},
					new ComplexConcOrNode(),
					new ComplexConcWordNode {Form = MakeVernString("yalola")}
				}});
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Gloss = MakeAnalysisString("green")});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 12, 15, null),  new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcTagNode {Tag = m_np});
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcMorphNode {Gloss = MakeAnalysisString("green")});
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_noun});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 12, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_noun});
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_verb});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));
		}

		[Test]
		public void GroupSearch()
		{
			var para = (IStTxtPara) m_text.ContentsOA.ParagraphsOS.First();
			ISegment seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcGroupNode {Minimum = 1, Maximum = 2, Children =
				{
					new ComplexConcWordNode {Category = m_verb},
					new ComplexConcOrNode(),
					new ComplexConcWordNode {Category = m_adj}
				}});
			model.Root.Children.Add(new ComplexConcWordNode {Category = m_noun});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 23, null)}).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcGroupNode {Minimum = 1, Maximum = -1, Children =
				{
					new ComplexConcWordNode {Category = m_verb},
					new ComplexConcGroupNode {Minimum = 0, Maximum = -1, Children =
						{
							new ComplexConcWordNode {Category = m_adj},
							new ComplexConcOrNode(),
							new ComplexConcWordNode {Category = m_noun}
						}},
				}});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 23, null)}).Using(m_fragmentComparer));
		}

		private class ParaFragmentEqualityComparer : IEqualityComparer<IParaFragment>
		{
			public bool Equals(IParaFragment x, IParaFragment y)
			{
				return x.Paragraph == y.Paragraph && x.GetMyBeginOffsetInPara() == y.GetMyBeginOffsetInPara() && x.GetMyEndOffsetInPara() == y.GetMyEndOffsetInPara();
			}

			public int GetHashCode(IParaFragment obj)
			{
				int code = 23;
				code = code * 31 + obj.Paragraph.GetHashCode();
				code = code * 31 + obj.GetMyBeginOffsetInPara();
				code = code * 31 + obj.GetMyEndOffsetInPara();
				return code;
			}
		}
	}
}
