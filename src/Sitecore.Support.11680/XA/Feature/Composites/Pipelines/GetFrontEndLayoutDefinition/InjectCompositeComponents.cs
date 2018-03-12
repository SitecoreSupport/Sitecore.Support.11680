namespace Sitecore.Support.XA.Feature.Composites.Pipelines.GetFrontEndLayoutDefinition
{
  using Sitecore;
  using Sitecore.Data;
  using Sitecore.Diagnostics;
  using Sitecore.XA.Feature.Composites.Extensions;
  using Sitecore.XA.Feature.Composites.Pipelines.GetFrontEndLayoutDefinition;
  using Sitecore.XA.Feature.Composites.Services;
  using Sitecore.XA.Foundation.Presentation.Layout;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Linq;
  using System.Xml;
  using System.Xml.Linq;

  public class InjectCompositeComponents : Sitecore.XA.Feature.Composites.Pipelines.GetFrontEndLayoutDefinition.InjectCompositeComponents
  {
    public InjectCompositeComponents(IOnPageEditingContextService onPageEditingContextService) : base(onPageEditingContextService) { }

    public override void Process(GetFrontEndLayoutDefinitionArgs args)
    {
      List<DeviceModel> collection = (from model in args.LayoutModel.Devices.DevicesCollection
                                      where model.DeviceId != Context.Device.ID
                                      select model).ToList();
      args.LayoutModel.Devices.DevicesCollection.RemoveAll((DeviceModel model) => model.DeviceId != Context.Device.ID);
      XElement xmlLayoutDefinition = this.OnPageEditingContextService.XmlLayoutDefinition;
      if (xmlLayoutDefinition != null)
      {
        DeviceModel currentDevice = this.GetCurrentDeviceModel(args);
        if (currentDevice != null)
        {
          DeviceModel deviceModel = (from d in new LayoutModel(xmlLayoutDefinition.ToString()).Devices.DevicesCollection
                                     where d.DeviceId == currentDevice.DeviceId
                                     select d).FirstOrDefault((DeviceModel d) => d.LayoutId == currentDevice.LayoutId);
          if (deviceModel != null)
          {
            currentDevice.Renderings.RenderingsCollection.Clear();
            List<RenderingModel> list = this.FilterRenderingsFromPartialDesign(deviceModel.Renderings.RenderingsCollection);
            List<ID> duplicatedUniqueIDs = list.GetDuplicatedRenderingsUniqueIDs().ToList();
            if (duplicatedUniqueIDs.Any())
            {
              list.RemoveAll((RenderingModel model) => duplicatedUniqueIDs.Any((ID id) => id == model.UniqueId));
              Log.Warn(string.Format("SXA: On page editing of composites functionality has filtered some renderings due to duplicates in layout definition. ({0})", string.Join(",", duplicatedUniqueIDs)), this);
            }
            #region Added code
            for (int i = 0; i < list.Count; i++)
            {
              NameValueCollection escapedParameters = new NameValueCollection();
              for (int j = 0; j < list[i].Parameters.Count; j++)
              {
                escapedParameters.Add(list[i].Parameters.AllKeys[j], System.Uri.EscapeDataString(list[i].Parameters[j]));
              }
              list[i].Parameters = escapedParameters;
            }
            #endregion Added code   
            list = this.RemoveRedundantRenderingsAttributes(list);
            currentDevice.Renderings.RenderingsCollection.AddRange(list);
          }
        }
      }
      args.LayoutModel.Devices.DevicesCollection.AddRange(collection);
    }
  }
}