using Atomus.Control.Localization.Models;
using Atomus.Database;
using Atomus.Service;
using System;
using System.Threading.Tasks;

namespace Atomus.Control.Localization.Controllers
{
    internal static class DefaultTranslatorController
    {
        internal static IResponse Search(this ICore core, DefaultTranslatorSearchModel search)
        {
            IServiceDataSet serviceDataSet;

            serviceDataSet = new ServiceDataSet
            {
                ServiceName = core.GetAttribute("ServiceName"),
                TransactionScope = false
            };
            serviceDataSet["Translator"].ConnectionName = core.GetAttribute("DatabaseName");
            serviceDataSet["Translator"].CommandText = core.GetAttribute("ProcedureID");
            serviceDataSet["Translator"].AddParameter("@SOURCE_LANGUAGE_TYPE", DbType.NVarChar, 10);
            serviceDataSet["Translator"].AddParameter("@TARGET_LANGUAGE_TYPE", DbType.NVarChar, 10);

            serviceDataSet["Translator"].NewRow();
            serviceDataSet["Translator"].SetValue("@SOURCE_LANGUAGE_TYPE", search.SOURCE_LANGUAGE_TYPE);
            serviceDataSet["Translator"].SetValue("@TARGET_LANGUAGE_TYPE", search.TARGET_LANGUAGE_TYPE);

            return core.ServiceRequest(serviceDataSet);
        }

        internal static async Task<IResponse> SaveAsync(this ICore core, string[] sources, string sourceCultureName)
        {
            IServiceDataSet serviceDataSet;

            serviceDataSet = new ServiceDataSet { ServiceName = core.GetAttribute("ServiceName") };
            serviceDataSet["LanguageDictionaryIns"].ConnectionName = core.GetAttribute("DatabaseName");
            serviceDataSet["LanguageDictionaryIns"].CommandText = core.GetAttribute("ProcedureSave");
            serviceDataSet["LanguageDictionaryIns"].AddParameter("@ACTION", DbType.NVarChar, 50);
            serviceDataSet["LanguageDictionaryIns"].AddParameter("@LANGUAGE_TEXT", DbType.NVarChar, 4000);
            serviceDataSet["LanguageDictionaryIns"].AddParameter("@LANGUAGE_TYPE", DbType.NVarChar, 10);
            serviceDataSet["LanguageDictionaryIns"].AddParameter("@USER_ID", DbType.Decimal, 18);

            foreach (string str in sources)
            {
                serviceDataSet["LanguageDictionaryIns"].NewRow();
                serviceDataSet["LanguageDictionaryIns"].SetValue("@ACTION", "Save");
                serviceDataSet["LanguageDictionaryIns"].SetValue("@LANGUAGE_TEXT", str);
                serviceDataSet["LanguageDictionaryIns"].SetValue("@LANGUAGE_TYPE", sourceCultureName);
                serviceDataSet["LanguageDictionaryIns"].SetValue("@USER_ID", Config.Client.GetAttribute("Account.USER_ID") == null ? DBNull.Value : Config.Client.GetAttribute("Account.USER_ID"));
            }

            return await core.ServiceRequestAsync(serviceDataSet);
        }
    }
}