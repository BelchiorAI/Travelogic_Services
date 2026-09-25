# Deploying for free: Render + Azure SQL + Backblaze B2

| Part | Service | Free plan |
| --- | --- | --- |
| Web app (`frontend/`) | Render static site | Free, always on, served from a CDN |
| API (`backend/`) | Render web service (Docker) | Free; sleeps after 15 minutes without requests, and the first request after that takes about a minute |
| Database | Azure SQL Database free offer | Free permanently (32 GB); pauses until next month if the monthly compute allowance runs out |
| Photos and videos | Backblaze B2 (S3-compatible) | 10 GB free, no card needed |
| AI import | Google Gemini | Your API key, stored as a Render secret |

Render can't host SQL Server itself: free services have 512 MB RAM (SQL Server needs about 2 GB) and no permanent disk. That's why the database and the files live in the two free services above. `render.yaml` at the repository root describes both Render services, so Render sets them up from the repo.

Total time: about 30 minutes. Keep a text file open to collect the values marked **Save**.

## 1. Database: Azure SQL (free offer)

1. Create a free account at https://azure.microsoft.com/free. It asks for a card to verify your identity; the free database offer doesn't charge.
2. In the Azure portal, search for **SQL databases**, then **Create**. On the banner **"Want to try Azure SQL Database for free?"** choose **Apply offer**.
3. Fill in:
   - **Resource group:** create one, e.g. `travelogic`.
   - **Database name:** `SuppliersDb`.
   - **Server:** *Create new*. Pick a unique name (e.g. `travelogic-suppliers-sql`), the **location closest to where you'll put Render** (e.g. *West Europe* for Render's Frankfurt region), and **Use SQL authentication** with an admin login and password. **Save** the server name, login and password.
   - **Behaviour when free limit is reached:** *Auto-pause the database until next month* (so it can never charge).
4. **Networking** tab: *Public endpoint*. Leave the firewall closed for now; step 4 opens it for Render.
5. **Review + create**, then **Create**. When it's done, open the database, go to **Connection strings**, copy the **ADO.NET** string, put your password in it, and add `Connection Timeout=60;` at the end (a paused free database takes a moment to wake). **Save** it. It looks like:

   ```
   Server=tcp:travelogic-suppliers-sql.database.windows.net,1433;Initial Catalog=SuppliersDb;Persist Security Info=False;User ID=<login>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;
   ```

The API creates the tables and loads six sample suppliers on its first start; you don't run anything in Azure.

## 2. Photos and videos: Backblaze B2

1. Create a free account at https://www.backblaze.com/sign-up/cloud-storage (no card).
2. **Buckets > Create a Bucket**:
   - **Name:** something unique, e.g. `travelogic-supplier-media-<your initials>`. **Save** it.
   - **Files in Bucket are:** **Private** (the API hands out short-lived signed links).
   - Create it, then **Save** the **Endpoint** shown on the bucket, e.g. `s3.eu-central-003.backblazeb2.com`. The part after `s3.` and before `.backblazeb2.com` is the **region** (here `eu-central-003`); **Save** that too.
3. **Application Keys > Add a New Application Key**:
   - **Name:** `supplier-api`.
   - **Allow access to Bucket(s):** your bucket. **Type of Access:** *Read and Write*.
   - Create it and **Save** the **keyID** and **applicationKey** straight away (the key is shown only once).

Browsers need permission (CORS) to upload to the bucket directly. The API sets this rule itself when it starts (step 5 shows how to check). If it can't, see *Troubleshooting* below.

## 3. Render: create both services from the Blueprint

1. Sign up at https://render.com with your GitHub account, and allow it to access the `Travelogic_Services` repository.
2. **New > Blueprint**, pick the repository, and Render reads `render.yaml`.
3. Fill in the secrets it asks for:

   | Setting | Value |
   | --- | --- |
   | `ConnectionStrings__SuppliersDb` | the Azure connection string from step 1 |
   | `Media__S3__ServiceUrl` | `https://` + the B2 endpoint, e.g. `https://s3.eu-central-003.backblazeb2.com` |
   | `Media__S3__Region` | the B2 region, e.g. `eu-central-003` |
   | `Media__S3__BucketName` | the bucket name |
   | `Media__S3__AccessKey` | the B2 **keyID** |
   | `Media__S3__SecretKey` | the B2 **applicationKey** |
   | `Ai__ApiKey` | your Gemini API key |

4. **Apply**. Render builds both services. Check their URLs on the dashboard: they should be `https://travelogic-supplier-api.onrender.com` and `https://travelogic-supplier-hub.onrender.com`. If Render added a suffix to either name, open that service's **Environment**, fix `VITE_API_BASE_URL` (web app) or `Cors__AllowedOrigins__0` and `Media__S3__CorsAllowedOrigins__0` (API) to the real URLs, and redeploy.

The API's first deploy will likely fail its health check because the Azure firewall is still closed. That's expected; the next step fixes it.

## 4. Let Render reach the database

1. In Render, open **travelogic-supplier-api > Connect > Outbound** and copy the listed IP addresses or ranges.
2. In Azure, open your **SQL server** (not the database) **> Networking > Firewall rules**, and add a rule for each Render address (for a range, enter its start and end). **Save**.
3. Back in Render: **travelogic-supplier-api > Manual Deploy > Deploy latest commit**.

## 5. Check it works

1. https://travelogic-supplier-api.onrender.com/health/ready shows `Healthy`, and https://travelogic-supplier-api.onrender.com/scalar/v1 shows the API docs.
2. In the API's **Logs**, look for `Media bucket CORS allows https://travelogic-supplier-hub.onrender.com`.
3. Open https://travelogic-supplier-hub.onrender.com: the six sample suppliers appear. Add a supplier, upload a photo on its page, and try **Import with AI** on the *Add supplier* page.

## What to expect on free plans

- **The API sleeps** after 15 minutes without requests. The next visitor waits about a minute while it starts (the web app shows loading placeholders), then it's fast again.
- **The database may be paused** (at the start of a visit after a quiet period, or for the rest of the month if the free allowance runs out). The API retries while it wakes.
- **Anyone with the link can use the site**: there are no user accounts yet (see *Known limitations* in [architecture.md](architecture.md)). The AI key stays secret on Render, but visitors can use AI import (limited to 10 requests per minute per visitor).
- New commits to `main` redeploy both services automatically.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API deploy fails, logs mention a SQL connection or login error | Firewall rules (step 4), or the connection string: server name, login, password, `Encrypt=True`. |
| Web app shows "We couldn't load…" or a network error | The API is waking up (wait a minute and retry), or `VITE_API_BASE_URL` doesn't match the API's real URL (fix it and redeploy the web app). |
| Browser console shows a CORS error calling the API | `Cors__AllowedOrigins__0` on the API must exactly match the web app's URL (with `https://`, no trailing slash). |
| Photo upload fails with a CORS error on `backblazeb2.com` | The API couldn't set the bucket's CORS rule (its log shows a warning). Set it once with Backblaze's command-line tool, see below. |
| AI import says "not switched on" | `Ai__ApiKey` is missing on the API service. |
| AI import fails with "could not be reached" | The model is overloaded (try again later) or the key is invalid. You can change `Ai__Model`, e.g. to `gemini-flash-latest`. |

**Setting the bucket's CORS rule by hand** (only needed if the API's log shows the CORS warning). Install Backblaze's tool (`pip install b2`), sign in with an application key that has access to all buckets, then run (all on one line, with your bucket name and web app URL):

```
b2 bucket update --cors-rules "[{\"corsRuleName\":\"supplier-hub\",\"allowedOrigins\":[\"https://travelogic-supplier-hub.onrender.com\"],\"allowedOperations\":[\"s3_put_object\",\"s3_get_object\",\"s3_head_object\"],\"allowedHeaders\":[\"*\"],\"exposeHeaders\":[\"ETag\"],\"maxAgeSeconds\":3600}]" <your-bucket-name> allPrivate
```
