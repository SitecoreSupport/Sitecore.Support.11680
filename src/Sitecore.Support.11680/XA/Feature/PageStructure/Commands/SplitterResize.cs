namespace Sitecore.Support.XA.Feature.PageStructure.Commands
{
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.DependencyInjection;
  using Sitecore.Layouts;
  using Sitecore.Shell.Applications.WebEdit.Commands;
  using Sitecore.Shell.Framework.Commands;
  using Sitecore.Text;
  using Sitecore.Web;
  using Sitecore.Web.UI.Sheer;
  using Sitecore.XA.Feature.PageStructure;
  using Sitecore.XA.Feature.PageStructure.Extensions;
  using Sitecore.XA.Feature.PageStructure.Models;
  using Sitecore.XA.Feature.PageStructure.Providers;
  using Sitecore.XA.Feature.PageStructure.Services;
  using Sitecore.XA.Foundation.Presentation.Layout;
  using System.Collections.Specialized;
  using System.Linq;

  public abstract class SplitterResize : Sitecore.XA.Feature.PageStructure.Commands.SplitterResize
  {
    private readonly int _delta;
    
    protected SplitterResize(int delta) : base(delta)
    {
      this._delta = delta;
    }

    public override void Execute(CommandContext context)
    {
      string formValue = WebUtil.GetFormValue("scLayout");
      string xml = WebEditUtil.ConvertJSONLayoutToXML(formValue);
      string id = ShortID.Decode(WebUtil.GetFormValue("scDeviceID"));
      LayoutDefinition layoutDefinition = LayoutDefinition.Parse(xml);
      if (layoutDefinition == null)
      {
        this.ReturnLayout(null);
      }
      else
      {
        DeviceDefinition device = layoutDefinition.GetDevice(id);
        if (device == null)
        {
          this.ReturnLayout(null);
        }
        else
        {
          RenderingDefinition renderingByUniqueId = device.GetRenderingByUniqueId(context.Parameters["referenceId"]);
          if (renderingByUniqueId == null)
          {
            this.ReturnLayout(null);
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
            EnabledPlaceholders enabledPlaceholders = new EnabledPlaceholders(nameValueCollection["EnabledPlaceholders"], Configuration.MaxSplitterSize);
            int num = enabledPlaceholders.PlaceholdersIndexes.Count + this._delta;
            if (num < this.MinSize || num > this.MaxSize)
            {
              this.ReturnLayout(null);
            }
            else
            {
              int num2;
              if (this._delta == 1)
              {
                enabledPlaceholders.AddPlaceholder();
                num2 = enabledPlaceholders.PlaceholdersIndexes.Last();
              }
              else
              {
                num2 = enabledPlaceholders.PlaceholdersIndexes.Last();
                enabledPlaceholders.RemovePlaceholder();
              }
              nameValueCollection["EnabledPlaceholders"] = enabledPlaceholders.ToString();
              IColumnSplitterConfigurationProvider service = ServiceLocator.ServiceProvider.GetService<IColumnSplitterConfigurationProvider>();
              if (this._delta == 1 && service.IsColumnSplitterRendering(ID.Parse(renderingByUniqueId.ItemID), null))
              {
                ServiceLocator.ServiceProvider.GetService<ISplittersGridParametersService>().FillGridDefaultValues(context.Items.First(), nameValueCollection, num2);
              }
              renderingByUniqueId.Parameters = new UrlString(nameValueCollection).GetUrl();
              LayoutModel layoutModel = new LayoutModel(layoutDefinition.ToXml());
              #region Added code
              for (int i = 0; i < layoutModel.Devices.DevicesCollection.Count; i++)
              {
                for (int j = 0; j < layoutModel.Devices.DevicesCollection[i].Renderings.RenderingsCollection.Count; j++)
                {
                  NameValueCollection escapedParameters = new NameValueCollection();
                  for (int k = 0; k < layoutModel.Devices.DevicesCollection[i].Renderings.RenderingsCollection[j].Parameters.Count; k++)
                  {
                    escapedParameters.Add(layoutModel.Devices.DevicesCollection[i].Renderings.RenderingsCollection[j].Parameters.AllKeys[k], System.Uri.EscapeDataString(layoutModel.Devices.DevicesCollection[i].Renderings.RenderingsCollection[j].Parameters[k]));
                  }
                  layoutModel.Devices.DevicesCollection[i].Renderings.RenderingsCollection[j].Parameters = escapedParameters;
                }
              }
              #endregion
              if (this._delta == -1 && service.IsColumnSplitterRendering(ID.Parse(renderingByUniqueId.ItemID), null))
              {
                layoutModel.RemoveRenderingsFromPlaceholder(num2, new ID(renderingByUniqueId.UniqueId), new ID(device.ID));
              }
              formValue = WebEditUtil.ConvertXMLLayoutToJSON(layoutModel.ToString());
              this.ReturnLayout(formValue);
            }
          }
        }
      }
    }
  }
}