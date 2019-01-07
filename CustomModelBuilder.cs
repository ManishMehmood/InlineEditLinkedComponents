using DD4T.ContentModel;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Tridion;
using System.Configuration;

namespace MyWebSite.Module
{
   public class CustomModelBuilder : Sdl.Web.Tridion.Mapping.DefaultModelBuilder
    {
        override public void BuildEntityModel(ref EntityModel entityModel, IComponent component, Type baseModelType, Localization localization)
        {


            using (new Tracer(entityModel, component, baseModelType, localization))
            {
                string[] schemaTcmUriParts = component.Schema.Id.Split('-');
                SemanticSchema semanticSchema = SemanticMapping.GetSchema(schemaTcmUriParts[1], localization);

                // The semantic mapping may resolve to a more specific model type than specified by the View Model itself (e.g. Image instead of just MediaItem for Teaser.Media)
                Type modelType = semanticSchema.GetModelTypeFromSemanticMapping(baseModelType);

                MappingData mappingData = new MappingData
                {
                    SemanticSchema = semanticSchema,
                    EntityNames = semanticSchema.GetEntityNames(),
                    TargetEntitiesByPrefix = GetEntityDataFromType(modelType),
                    Content = component.Fields,
                    Meta = component.MetadataFields,
                    TargetType = modelType,
                    SourceEntity = component,
                    Localization = localization
                };
              
                entityModel = (EntityModel)CreateViewModel(mappingData);
                entityModel.Id = GetDxaIdentifierFromTcmUri(component.Id);
                if (localization.IsStaging)
                {
                    entityModel.XpmMetadata = GetXpmMetadata(component);
                }
                if (entityModel is MediaItem && component.Multimedia != null && component.Multimedia.Url != null)
                {
                    MediaItem mediaItem = (MediaItem)entityModel;
                    mediaItem.Url = component.Multimedia.Url;
                 
                    mediaItem.FileName = component.Multimedia.FileName;
                    mediaItem.FileSize = component.Multimedia.Size;
                    mediaItem.MimeType = component.Multimedia.MimeType;
                }

                if (entityModel is EclItem)
                {
                    // MapEclItem((EclItem)entityModel, component);t
                    throw new Exception("ECL not supported. Please extend the CustomModelBuilder");//TODO
                }

                if (entityModel is Link)
                {
                    Link link = (Link)entityModel;
                    if (String.IsNullOrEmpty(link.Url))
                    {
                        link.Url = SiteConfiguration.LinkResolver.ResolveLink(component.Id);
                    }
                }

                // Set the Entity Model's default View (if any) after it has been fully initialized.
                entityModel.MvcData = entityModel.GetDefaultView(localization);
            }
        }
        internal static string GetDxaIdentifierFromTcmUri(string tcmUri, string templateTcmUri = null)
        {
            // Return the Item (Reference) ID part of the TCM URI.
            string result = tcmUri.Split('-')[1];
            if (templateTcmUri != null)
            {
                result += "-" + templateTcmUri.Split('-')[1];
            }
            return result;
        }

        protected virtual IDictionary<string, object> GetXpmMetadata(IComponent cp)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            result.Add("ComponentID", cp.Id);
            result.Add("ComponentModified", cp.RevisionDate.ToString("yyyy-MM-ddTHH:mm:ss"));
            result.Add("ComponentTemplateID", WebRequestContext.Localization.GetResources()["core.xpmDummyCTID"]);//set to dummy template id
            result.Add("ComponentTemplateModified", "2018-03-21T10:39:58" /*cp.ComponentTemplate.RevisionDate.ToString("yyyy-MM-ddTHH:mm:ss")*/); //set to dummy timestamp
            result.Add("IsRepositoryPublished", false);
            return result;
        }
    }
}
