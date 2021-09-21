using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Hmm.Automobile.Tests
{
    public class AutoTestFixtureBase : TestFixtureBase
    {
        protected readonly XNamespace XmlNamespace = @"http://schema.hmm.com/2020";
        private Author _user;

        protected const string NoteXmlTextBase =
            "<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{0}\"><Content>{1}</Content></Note>";

        protected Author DefaultAuthor => _user ??= LookupRepo.GetEntities<Author>().FirstOrDefault();

        protected override void InsertSeedRecords(
            List<Subsystem> systems = null,
            List<Author> authors = null,
            List<NoteRender> renders = null,
            List<NoteCatalog> catalogs = null)
        {
            systems ??= new List<Subsystem>();
            renders ??= new List<NoteRender>();
            catalogs ??= new List<NoteCatalog>();

            systems.Add(
                new Subsystem
                {
                    Name = "Automobile",
                    Description = "A Basic information manage system for the car"
                });

            renders.Add(
                new NoteRender
                {
                    Name = "GasLog",
                    Namespace = "Hmm.Renders",
                    Description = "Testing default note render"
                });

            catalogs.Add(
                new NoteCatalog
                {
                    Name = AutomobileConstant.GasLogCatalogName,
                    Schema = File.ReadAllText("GasLog.xsd"),
                    Subsystem = new Subsystem { Name = "default subsystem" },
                    Render = renders[0],
                    Description = "Testing catalog"
                });
            catalogs.Add(
                new NoteCatalog
                {
                    Name = AutomobileConstant.AutoMobileInfoCatalogName,
                    Schema = File.ReadAllText("Automobile.xsd"),
                    Subsystem = new Subsystem { Name = "default subsystem" },
                    Render = renders[0],
                    Description = "Testing automobile note"
                });
            catalogs.Add(
                new NoteCatalog
                {
                    Name = AutomobileConstant.GasDiscountCatalogName,
                    Schema = File.ReadAllText("Discount.xsd"),
                    Subsystem = new Subsystem { Name = "default subsystem" },
                    Render = renders[0],
                    Description = "Testing discount note"
                });
            base.InsertSeedRecords(systems, authors, renders, catalogs);
        }
    }
}