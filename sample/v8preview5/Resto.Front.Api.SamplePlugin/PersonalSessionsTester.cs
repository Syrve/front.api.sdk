using System.Linq;

namespace Resto.Front.Api.SamplePlugin
{
    internal static class PersonalSessionsTester
    {
        public static void Test()
        {
            // Before use, update OperationServiceExtensions.Pin with some user's pin
            var credentials = PluginContext.Operations.GetCredentials();
            var user = PluginContext.Operations.GetUser(credentials);
            PluginContext.Log.Info($"Testing personal sessions for {user.Name}");

            if (user.IsSessionOpen)
            {
                var result = PluginContext.Operations.ClosePersonalSession(credentials);
                PluginContext.Log.Info($"Personal session for {user.Name} closed: {result}");
                return;
            }

            if (!PluginContext.Operations.GetHostRestaurant().UsePersonalRoles)
            {
                var result = PluginContext.Operations.OpenPersonalSession(credentials);
                PluginContext.Log.Info($"Personal session for {user.Name} opened: {result}");
                return;
            }

            var roles = user.GetUserRoles();
            if (roles.IsEmpty())
            {
                PluginContext.Log.Info("User has no available roles, so he cannot open personal session.");
                return;
            }

            PluginContext.Log.Info($"User has the following roles: {string.Join(", ", roles.Select(role => role.Name))}");
            {
                var currentRole = roles.First(); // if there are several roles, the user must choose who he will work today
                var result = PluginContext.Operations.OpenPersonalSession(credentials, currentRole);
                PluginContext.Log.Info($"Personal session for {user.Name} with role {currentRole.Name} opened: {result}");
            }
        }
    }
}
