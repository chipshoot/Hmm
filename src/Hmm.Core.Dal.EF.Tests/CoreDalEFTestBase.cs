using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Collections.Generic;

namespace Hmm.Core.Dal.EF.Tests
{
    public class CoreDalEFTestBase : DbTestFixtureBase
    {
        protected void SetupTestingEnv()
        {
            var authors = new List<Author>
            {
                new Author
                {
                    AccountName = "jfang",
                    IsActivated = true,
                    Description = "testing author"
                },
                new Author
                {
                    AccountName = "awang",
                    IsActivated = true,
                    Description = "testing author"
                }
            };

            var systems = new List<Subsystem>
            {
                new Subsystem
                {
                    Name = "HmmNote",
                    DefaultAuthor = authors[0],
                    Description = "HMM note management"
                },

                new Subsystem
                {
                    Name = "Automobile",
                    DefaultAuthor = authors[0],
                    Description = "Car information management"
                }
            };
            var renders = new List<NoteRender>
            {
                new NoteRender
                {
                    Name = "DefaultNoteRender",
                    Namespace = "Hmm.Renders",
                    IsDefault = true,
                    Description = "Testing default note render"
                },
                new NoteRender
                {
                    Name = "GasLog",
                    Namespace = "Hmm.Renders",
                    Description = "Testing default note render"
                }
            };
            var catalogs = new List<NoteCatalog>
            {
                new NoteCatalog
                {
                    Name = "DefaultNoteCatalog",
                    Schema = "DefaultSchema",
                    Render = renders[0],
                    Subsystem = systems[0],
                    IsDefault = true,
                    Description = "Testing catalog"
                },
                new NoteCatalog
                {
                    Name = "Gas Log",
                    Schema = "GasLogSchema",
                    Render = renders[1],
                    Subsystem = systems[1],
                    Description = "Testing catalog"
                }
            };

            SetupRecords(authors, renders, catalogs, systems);
        }
    }
}