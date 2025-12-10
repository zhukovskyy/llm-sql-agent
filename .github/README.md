# 🚀 GitHub Actions Auto-Deploy Setup

This repository uses GitHub Actions for automatic deployment to production.

## 🔧 Quick Setup (5 minutes)

### 1. Add GitHub Secrets

Go to: **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

Add these 4 secrets:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `OPENAI_API_KEY` | Your OpenAI API key | `sk-proj-...` |
| `DATABASE_CONNECTION_STRING` | SQL Server connection string | `Server=...` |
| `DEPLOY_USERNAME` | Web Deploy username | `DOMAIN\Username` |
| `DEPLOY_PASSWORD` | Web Deploy password | `your_password` |

### 2. That's it! 🎉

Now every time you push to `main`, your app will automatically deploy!

```bash
git add .
git commit -m "Your changes"
git push
```

Watch the deployment: [Actions Tab](../../actions)

---

## 📝 Manual Deployment

1. Go to [Actions Tab](../../actions)
2. Select **"Deploy to Production"**
3. Click **"Run workflow"**
4. Click green **"Run workflow"** button

---

## 🔍 What Happens During Deploy?

1. ✅ Checkout code
2. ✅ Setup .NET 8
3. ✅ Generate `appsettings.json` from secrets
4. ✅ Restore dependencies
5. ✅ Build project
6. ✅ Run tests (if any)
7. ✅ Publish artifacts
8. ✅ Deploy to IIS via Web Deploy

---

## 🛡️ Security

- ✅ Secrets are encrypted by GitHub
- ✅ Never exposed in logs or code
- ✅ Can be rotated anytime
- ✅ `appsettings.json` never committed

---

## 📊 Check Deployment Status

- **Live site:** http://bai.a95.biz:80/
- **Deployment logs:** [Actions Tab](../../actions)
- **Last deploy:** ![Deploy Status](https://github.com/zhukovskyy/llm-sql-agent/actions/workflows/deploy.yml/badge.svg)

---

## 🐛 Troubleshooting

### Deployment fails?
1. Check [Actions logs](../../actions) for errors
2. Verify all 4 secrets are set correctly
3. Make sure deployment password is correct

### Need to update secrets?
1. Go to repository **Settings** → **Secrets**
2. Click on secret name
3. Update value
4. Re-run workflow

---

## 💡 Pro Tips

- Use `workflow_dispatch` for manual deploys
- Check logs in real-time during deployment
- Set up Slack/Email notifications for failed deploys
- Add staging environment later

---

Need help? Check the [workflow file](.github/workflows/deploy.yml)
