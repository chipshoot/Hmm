using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Collections.Generic;

namespace Hmm.Core.Dal.EF.Tests
{
    public class CoreDalEfTestBase : DbTestFixtureBase
    {
        protected void SetupTestingEnv()
        {
            var authors = new List<Author>
            {
                new()
                {
                    AccountName = "jfang",
                    IsActivated = true,
                    Description = "testing author"
                },
                new()
                {
                    AccountName = "awang",
                    IsActivated = true,
                    Description = "testing author"
                }
            };

            var systems = new List<Subsystem>
            {
                new()
                {
                    Name = "HmmNote",
                    DefaultAuthor = authors[0],
                    Description = "HMM note management"
                },

                new()
                {
                    Name = "Automobile",
                    DefaultAuthor = authors[0],
                    Description = "Car information management"
                }
            };
            var renders = new List<NoteRender>
            {
                new()
                {
                    Name = "DefaultNoteRender",
                    Namespace = "Hmm.Renders",
                    IsDefault = true,
                    Description = "Testing default note render"
                },
                new()
                {
                    Name = "GasLog",
                    Namespace = "Hmm.Renders",
                    Description = "Testing default note render"
                }
            };
            var catalogs = new List<NoteCatalog>
            {
                new()
                {
                    Name = "DefaultNoteCatalog",
                    Schema = "DefaultSchema",
                    Render = renders[0],
                    Subsystem = systems[0],
                    IsDefault = true,
                    Description = "Testing catalog"
                },
                new()
                {
                    Name = "Gas Log",
                    Schema = "GasLogSchema",
                    Render = renders[1],
                    Subsystem = systems[1],
                    Description = "Testing catalog"
                }
            };
            systems[0].NoteCatalogs = new List<NoteCatalog> { catalogs[0] };
            systems[1].NoteCatalogs = new List<NoteCatalog> { catalogs[1] };

            SetupRecords(authors, renders, catalogs, systems);
        }
    }
}