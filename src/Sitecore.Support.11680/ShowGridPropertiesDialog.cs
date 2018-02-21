namespace Sitecore.Support.XA.Foundation.Grid.Commands
{
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.DependencyInjection;
  using Sitecore.Layouts;
  using Sitecore.Shell.Applications.ContentEditor;
  using Sitecore.Text;
  using Sitecore.Web;
  using Sitecore.XA.Foundation.SitecoreExtensions.Repositories;
  using System;
  using System.Collections.Specialized;

  public class ShowGridPropertiesDialog : Sitecore.XA.Foundation.Grid.Commands.ShowGridPropertiesDialog
  {
    protected override void UpdateLayout(NameValueCollection contextParameters, FieldEditorOptions fieldEditorOptions)
    {
      string placeholder = contextParameters["placeHolderKey"];
      string uniqueId = Guid.Parse(contextParameters["renderingUid"]).ToString("B").ToUpperInvariant();
      string text = contextParameters["fieldName"];
      LayoutDefinition layoutDefinition = this.GetLayoutDefinition();
      if (layoutDefinition == null)
      {
        this.ReturnLayout(null, null, null);
      }
      else
      {
        string id = ShortID.Decode(WebUtil.GetFormValue("scDeviceID"));
        DeviceDefinition device = layoutDefinition.GetDevice(id);
        if (device == null)
        {
          this.ReturnLayout(null, null, null);
        }
        else
        {
          RenderingDefinition renderingByUniqueId = device.GetRenderingByUniqueId(uniqueId);
          if (renderingByUniqueId == null)
          {
            this.ReturnLayout(null, null, null);
          }
          else
          {
            if (string.IsNullOrEmpty(renderingByUniqueId.Parameters))
            {
              if (!string.IsNullOrEmpty(renderingByUniqueId.ItemID))
              {
                RenderingItem renderingItem = Client.ContentDatabase.GetItem(renderingByUniqueId.ItemID);
                renderingByUniqueId.Parameters = ((renderingItem != null) ? renderingItem.Parameters : string.Empty);
              }
              else
              {
                renderingByUniqueId.Parameters = string.Empty;
              }
            }
            NameValueCollection nameValueCollection = WebUtil.ParseUrlParameters(renderingByUniqueId.Parameters);
            foreach (FieldDescriptor field in fieldEditorOptions.Fields)
            {
              Item item = ServiceLocator.ServiceProvider.GetService<IContentRepository>().GetItem(field.FieldID);
              if (text == item.Name)
              {
                this.FillGridParameters(contextParameters, device, nameValueCollection, text, field.Value);
              }
              else
              {
                nameValueCollection[item.Name] = field.Value;
              }
            }
            renderingByUniqueId.Parameters = new UrlString(nameValueCollection).GetUrl();
            string layout = WebEditUtil.ConvertXMLLayoutToJSON(layoutDefinition.ToXml());
            this.ReturnLayout(layout, renderingByUniqueId.UniqueId, placeholder);
          }
        }
      }
    }
  }
}