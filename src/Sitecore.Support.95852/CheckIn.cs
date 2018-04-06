namespace Sitecore.Support.Shell.Framework.Commands
{
  using Sitecore;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Web.UI.Sheer;
  using System;
  using System.Collections.Specialized;
  using Sitecore.Shell.Framework.Commands;

  [Serializable]
  public class CheckIn : Command
  {
    public override void Execute(CommandContext context)
    {
      Assert.ArgumentNotNull(context, "context");
      if (context.Items.Length == 1)
      {
        Item item = context.Items[0];
        NameValueCollection parameters = new NameValueCollection();
        parameters["id"] = item.ID.ToString();
        parameters["language"] = item.Language.ToString();
        parameters["version"] = item.Version.ToString();
        Context.ClientPage.Start(this, "Run", parameters);
      }
    }

    public override CommandState QueryState(CommandContext context)
    {
      Assert.ArgumentNotNull(context, "context");
      if (context.Items.Length != 1)
      {
        return CommandState.Hidden;
      }
      Item item = context.Items[0];
      if (Context.IsAdministrator)
      {
        if (!item.Locking.IsLocked())
        {
          return CommandState.Hidden;
        }
        return CommandState.Enabled;
      }
      if (item.Appearance.ReadOnly)
      {
        return CommandState.Disabled;
      }
      if (!item.Access.CanWrite())
      {
        return CommandState.Disabled;
      }
      if (!item.Locking.HasLock())
      {
        #region Added code
        SheerResponse.Eval("window.parent.location.reload();");
        #endregion
        return CommandState.Disabled;
      }
      if (!item.Access.CanWriteLanguage())
      {
        return CommandState.Disabled;
      }
      return base.QueryState(context);
    }

    protected void Run(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      #region Added code
      CheckModifiedParameters p = new CheckModifiedParameters { ResumePreviousPipeline = true };
      #endregion
      #region Modified code
      // The fix: wait until saveUI pipeline finishes the work
      if (SheerResponse.CheckModified(p))
      #endregion
      {
        string itemPath = args.Parameters["id"];
        string name = args.Parameters["language"];
        string str3 = args.Parameters["version"];
        Item item = Client.GetItemNotNull(itemPath, Language.Parse(name), Sitecore.Data.Version.Parse(str3));
        if (item.Locking.HasLock() || Context.IsAdministrator)
        {
          string[] parameters = new string[] { AuditFormatter.FormatItem(item) };
          Log.Audit(this, "Check in: {0}", parameters);
          item.Editing.BeginEdit();
          item.Locking.Unlock();
          item.Editing.EndEdit();
          Context.ClientPage.SendMessage(this, "item:checkedin");
        }
      }
    }
  }
}
