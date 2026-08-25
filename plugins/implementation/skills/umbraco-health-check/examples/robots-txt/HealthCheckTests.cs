using System.Net;

namespace Umbraco_CMS.Skills.TestHost;

[TestFixture]
public sealed class HealthCheckTests
{
    [Test]
    public async Task Robots_check_reports_missing_file_and_can_create_it()
    {
        HttpClient client = ReferenceSiteFixture.Client;
        await client.DeleteAsync("/example/health-check/robots");

        try
        {
            HttpResponseMessage initial = await client.GetAsync("/example/health-check/robots");
            Assert.That(initial.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await initial.Content.ReadAsStringAsync(), Is.EqualTo("Error|False"));

            HttpResponseMessage action =
                await client.GetAsync("/example/health-check/robots?action=addDefaultRobotsTxtFile");
            Assert.That(action.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await action.Content.ReadAsStringAsync(), Is.EqualTo("Success|True"));
        }
        finally
        {
            await client.DeleteAsync("/example/health-check/robots");
        }
    }
}
