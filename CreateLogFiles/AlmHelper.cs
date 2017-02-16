using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HP.ALM.QC.OTA.Entities.Api;
using Mercury.TD.Client.Ota.Api;
using Mercury.TD.Client.Ota.Core;

namespace Avenger
{
    public class AlmHelper
    {
        public ISite Site { get; set; }
        public IPrivateSite PrivateSite { get; set; }
        public IConnection Connection { get; set; }
        public EntityType EntityType { get; set; }
        public IEntityList FilteredEntities { get; set; }

        internal void ConnectToAlmServerByUrl(string almUrl)
        {
            Site = Application.Connect(almUrl);
        }

        internal void AuthenticateAlmUser(ISite site, string userName, string password, bool getVisibleProjects)
        {
            PrivateSite = site.Authenticate(userName, password, getVisibleProjects);
        }

        internal List<string> GetDomainNames()
        {
            return PrivateSite.Domains.Select(domain => domain.Value.Name).ToList();
        }

        internal List<string> GetProjectNames(string domainName)
        {
            var projectNames = new List<string>();
            foreach (var domain in PrivateSite.Domains.Where(domain => domain.Value.Name.Equals(domainName)))
            {
                projectNames.AddRange(domain.Value.Projects.Select(project => project.Value.Name));
            }
            return projectNames;
        }

        internal void LoginToAlmProject(string domain, string project)
        {
            Connection = PrivateSite.Connect(domain, project);
        }

        internal List<string> GetFavoriteFilters()
        {
            var favoriteFilters = new List<string>(); 
            var moduleId = EntityTypeHelper.GetModuleIdByEntityType(EntityType).ToString();
            IFactory favoriteFoldersFactory = Connection.GetFactory<IFavoriteFolder, IFavoriteFolder>();
            var favoriteFolders = favoriteFoldersFactory.NewList(favoriteFoldersFactory.NewListConfiguration());
            foreach (var ff in favoriteFolders)
            {
                var parentFolder = ff as IFavoriteFolder;
                if (parentFolder == null || parentFolder.ModuleID != moduleId || parentFolder.Name != "Avenger")
                    continue;
                var favoritesFactory = Connection.GetFactory<IFavorite, IFavoriteFolder>();
                var fs = favoritesFactory.NewList(favoritesFactory.NewListConfiguration(), parentFolder);
                favoriteFilters.AddRange(fs.Select(f => f.Name));
            }
            return favoriteFilters;
        }

        internal List<string> GetEntityFieldNames()
        {
            var values = new List<string>();
            switch (EntityType)
            {
                case EntityType.Defect:
                    var defectsFactory = Connection.GetFactory<IBug>();
                    foreach (var field in defectsFactory.Metadata.Fields.Values.Where(f => !f.Type.DefaultType.Equals(typeof(DateTime))))
                    {
                        values.Add(field.Appearance.Label);
                    }
                    break;
                case EntityType.Requirement:
                    var requirementsFactory = Connection.GetFactory<IRequirement>();
                    foreach (var field in requirementsFactory.Metadata.Fields.Values.Where(f => !f.Type.DefaultType.Equals(typeof(DateTime))))
                    {
                        values.Add(field.Appearance.Label);
                    }
                    break;
            }
            return values;
        }

        internal IList<IFavorite> GetFavorites()
        {
            List<IFavorite> favorites = new List<IFavorite>();

            var moduleId = EntityTypeHelper.GetModuleIdByEntityType(EntityType).ToString();
            IFactory favoriteFoldersFactory = Connection.GetFactory<IFavoriteFolder, IFavoriteFolder>();
            var favoriteFolders = favoriteFoldersFactory.NewList(favoriteFoldersFactory.NewListConfiguration());
            foreach (var ff in favoriteFolders)
            {
                var parentFolder = ff as IFavoriteFolder;
                if (parentFolder == null || parentFolder.ModuleID != moduleId || parentFolder.Name != "Avenger")
                    continue;
                var favoritesFactory = Connection.GetFactory<IFavorite, IFavoriteFolder>();
                var fs = favoritesFactory.NewList(favoritesFactory.NewListConfiguration(), parentFolder);
                foreach (var favorite in fs)
                {
                    favorites.Add(favorite);
                }
            }
            return favorites;
        }

        internal IEntityList GetEntetiesAfterFilterApplied(string favoriteFilter)
        {
            IFavorite favorite = GetFavorites().Where(fav => fav.Name.Equals(favoriteFilter)).FirstOrDefault();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in favorite.ListConfigurationData)
            {
                stringBuilder.Append(c);
                if (c == '>')
                {
                    stringBuilder.Append("\n");
                }
            }
            string xmlData = string.Format("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + "\n" + "<root>" + "\n" + stringBuilder.ToString() + "</root>");
            DataHandler dataHandler = new DataHandler();
            dataHandler.CreateNewXdocumentByString(xmlData);
            Dictionary<string, string> filterFieldsAndValues = dataHandler.GetFieldsAndValuesFromXml();
            FilteredEntities = GetEntetiesAfterFiltering(filterFieldsAndValues);
            return FilteredEntities;
        }

        internal IEntityList GetEntetiesAfterFiltering(Dictionary<string, string> fieldsAndValues)
        {
            IFactory factory = null;
            switch (EntityType)
            {
                case EntityType.Defect:
                    {
                        factory = Connection.GetFactory<IBug>();
                        break;

                    }
                case EntityType.Requirement:
                    {
                        factory = Connection.GetFactory<IRequirement>();
                        break;
                    }
            }
            if (factory != null)
            {
                IListConfiguration listConfiguration = factory.NewListConfiguration();
                foreach (var fieldAndValue in fieldsAndValues)
                {
                    listConfiguration.Filter[fieldAndValue.Key] = fieldAndValue.Value;
                }
                return factory.NewList(listConfiguration);
            }
            return null;
        }

        internal void DisposeConnection()
        {
            if (Connection != null)
                Connection.Dispose();
            if (PrivateSite != null)
                PrivateSite.Dispose();
            if (Site != null)
                Site.Dispose();
        }
    }
}
