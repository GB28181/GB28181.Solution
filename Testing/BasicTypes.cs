using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using Xunit;
using Xunit.Abstractions;

namespace Testing
{

    public class MockData
    {
        public IList<string> DataSets { get; set; } = new List<string>();

        public MockData()
        {
            DataSets.Add("helloword");
        }
    }


    public class BasicTypes
    {

        protected readonly ITestOutputHelper Output;

        public BasicTypes(ITestOutputHelper tempOutput)
        {
            Output = tempOutput;
        }

        [Fact]
        public void TestMockData()
        {
            var md = new MockData();

            md.DataSets.Count();


            Assert.Single(md.DataSets);

            Output.WriteLine($"count :{md.DataSets.Count()}");

            md.DataSets.Add("apple");

            Assert.Equal(2, md.DataSets.Count());

            md.DataSets.Add("peach");

            Output.WriteLine($"count :{md.DataSets.Count()}");

        }
    }
}
