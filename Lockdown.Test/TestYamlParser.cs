using System;
using System.Threading.Tasks;
using Lockdown.Build.Utils;
using Xunit;
using Raw = Lockdown.Build.RawEntities;
using Shouldly;

namespace Lockdown.Test
{
    public class TestYamlParser
    {

        YamlParser yamlParser;
        public TestYamlParser()
        {
            this.yamlParser = new YamlParser();
        }

        [Fact]
        public void TestParseSimple()
        {
            var yamlString = @"title: Russian hardbass
date: 2021-02-20";


            var rawMetadata = this.yamlParser.Parse<Raw.PostMetadata>(yamlString);

            rawMetadata.Title.ShouldBe("Russian hardbass");
            rawMetadata.Date.ShouldBe(new DateTime(2021, 2, 20));
        }

        [Fact]
        public void TestParseSimpleNoExtras()
        {
            var yamlString = @"title: Russian hardbass
date: 2021-02-20
this: is extra
tags:
 - something
 - is wrong";


            var rawMetadata = this.yamlParser.Parse<Raw.PostMetadata>(yamlString);

            rawMetadata.Title.ShouldBe("Russian hardbass");
            rawMetadata.Date.ShouldBe(new DateTime(2021, 2, 20));
        }

        [Fact]
        public void TestParseExtras()
        {
            var yamlString = @"title: Russian hardbass
date: 2021-02-20
extra: is extra info
tags:
 - something
 - is wrong";


            var rawMetadata = this.yamlParser.ParseExtras<Raw.PostMetadata>(yamlString);

            rawMetadata.Title.ShouldBe("Russian hardbass");
            rawMetadata.Date.ShouldBe(new DateTime(2021, 2, 20));
            Assert.Equal(rawMetadata.Extras.title, rawMetadata.Title);
            Assert.Equal(rawMetadata.Extras.extra, "is extra info");
            Assert.True(rawMetadata.Extras.tags == string.Empty);
            Assert.True(rawMetadata.Extras.nonExistent == null);
        }
    }
}
