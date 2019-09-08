package aaa.QuestAppLauncher.App;

import com.unity3d.player.UnityPlayerActivity;
import android.app.Activity;
import android.app.AppOpsManager;
import android.app.usage.UsageStats;
import android.app.usage.UsageStatsManager;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.pm.PackageManager.NameNotFoundException;
import android.content.pm.PackageInfo;
import android.content.pm.FeatureInfo;
import android.content.pm.ApplicationInfo;
import android.provider.Settings;
import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.util.Calendar;
import java.util.Map;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;
import java.util.zip.ZipOutputStream;
import android.os.Bundle;
import android.util.Log;
import android.graphics.Bitmap;
import android.graphics.drawable.BitmapDrawable;
import java.util.List;
import java.util.LinkedList;

class AppInfoInternal {
    public ApplicationInfo app;
    public long lastTimeUsed;
}

public class AppInfo extends UnityPlayerActivity {

    private static final String TAG = "AppInfo";
    private List<AppInfoInternal> installedApps;

    @Override
    protected void onStart() {
        super.onStart();

        installedApps = new LinkedList<AppInfoInternal>();
        for(ApplicationInfo app : this.getPackageManager().getInstalledApplications(PackageManager.GET_META_DATA)) {
            if((app.flags & (ApplicationInfo.FLAG_UPDATED_SYSTEM_APP | ApplicationInfo.FLAG_SYSTEM)) > 0) {
                // Skip system app
                continue;
            }

            AppInfoInternal appInfoInternal = new AppInfoInternal();
            appInfoInternal.app = app;
            installedApps.add(appInfoInternal);
        }
    }

    public int getSize() {
        return this.installedApps.size();
    }

    public String getPackageName(int i) {
        return this.installedApps.get(i).app.packageName;
    }

    public String getAppName(int i) {
        return (String)this.getPackageManager().getApplicationLabel(installedApps.get(i).app);
    }

    public long getLastTimeUsed(int i)
    {
        return this.installedApps.get(i).lastTimeUsed;
    }

    public boolean isQuestApp(int i) {
        try {
            PackageInfo info = this.getPackageManager().getPackageInfo(getPackageName(i), PackageManager.GET_CONFIGURATIONS);
            if (null == info.reqFeatures) {
                return false;
            }
            for (FeatureInfo f : info.reqFeatures) {
                if (f.name != null && f.name.equals("android.hardware.vr.headtracking")) {
                     return true;
                }
            }
        } catch (NameNotFoundException e) {
            e.printStackTrace();
        }
        return false;
    }

    public boolean is2DApp(int i)
    {
        ApplicationInfo app = this.installedApps.get(i).app;
        if (null == app.metaData)
        {
            return true;
        }

        String vrAppMode = app.metaData.getString("com.samsung.android.vr.application.mode");
        if (null == vrAppMode || !vrAppMode.equals("vr_only")) {
            return true;
        }

        return false;
    }

    public byte[] getIcon(int i) {
        BitmapDrawable icon = (BitmapDrawable)this.getPackageManager().getApplicationIcon(installedApps.get(i).app);
        Bitmap bmp = icon.getBitmap();
        ByteArrayOutputStream stream = new ByteArrayOutputStream();
        bmp.compress(Bitmap.CompressFormat.JPEG, 100, stream);
        byte[] byteArray = stream.toByteArray();
        return byteArray;
    }

    public boolean hasUsageStatsPermissions() {
        AppOpsManager appOps = (AppOpsManager) this.getSystemService(Context.APP_OPS_SERVICE);
        final int mode = appOps.checkOpNoThrow(AppOpsManager.OPSTR_GET_USAGE_STATS, android.os.Process.myUid(), this.getPackageName());
        boolean granted = mode == AppOpsManager.MODE_DEFAULT ?
            (this.checkCallingOrSelfPermission(android.Manifest.permission.PACKAGE_USAGE_STATS) == PackageManager.PERMISSION_GRANTED)
            : (mode == AppOpsManager.MODE_ALLOWED);
        return granted;
    }

    public void grantUsageStatsPermission() {
        startActivity(new Intent(Settings.ACTION_USAGE_ACCESS_SETTINGS));
    }

    public void processLastTimeUsed(int numDaysLookback) {
        if (!hasUsageStatsPermissions()) {
            Log.i(TAG, "PorcessLastTimeUsed: No permissions, so skipping");
        }

        UsageStatsManager usageStatsManager = (UsageStatsManager) this.getSystemService(Context.USAGE_STATS_SERVICE);
        Calendar calendar = Calendar.getInstance();
        calendar.add(Calendar.DAY_OF_MONTH, -1 * numDaysLookback);
        long start = calendar.getTimeInMillis();
        long end = System.currentTimeMillis();
        Map<String, UsageStats> stats = usageStatsManager.queryAndAggregateUsageStats(start, end);

        for (int i = 0; i < this.installedApps.size(); i++) {
            if (stats.containsKey(getPackageName(i))) {
                AppInfoInternal app = this.installedApps.get(i);
                app.lastTimeUsed = stats.get(getPackageName(i)).getLastTimeStamp();
                Log.v(TAG, "Package " + getPackageName(i) + " last time stamp = " + app.lastTimeUsed);
                this.installedApps.set(i, app);
            }
        }
    }

    public static void unzip(String zipFileName, String targetPath) {
        File outDir = new File(targetPath);

        // Create target path if not exist
        createDirIfNotExist(outDir);

        byte[] buffer = new byte[8192];
        try (FileInputStream fis = new FileInputStream(zipFileName);
                BufferedInputStream bis = new BufferedInputStream(fis);
                ZipInputStream stream = new ZipInputStream(bis)) {

            ZipEntry entry = null;
            while ((entry = stream.getNextEntry()) != null) {

                File filePath = new File(outDir, entry.getName());
                Log.v(TAG, "Unzipping " + filePath);

                if (entry.isDirectory()) {
                    // Create dir if required while unzipping
                    createDirIfNotExist(filePath);
                } else {
                    try (FileOutputStream fos = new FileOutputStream(filePath);
                            BufferedOutputStream bos = new BufferedOutputStream(fos, buffer.length)) {
                        int len;
                        while ((len = stream.read(buffer)) > 0) {
                            bos.write(buffer, 0, len);
                        }
                    }
                }

                stream.closeEntry();
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public static void addFileToZip(String zipFilePath, String sourceFilePath, String entryName)
    {
        Log.v(TAG, "Adding to zip: " + sourceFilePath + " to " + zipFilePath + " with entry name " + entryName);

        File zipFile = new File(zipFilePath);
        File tempZipFile;
        byte[] buffer = new byte[8192];

        try {
            // Create temporary zip file in same location as zip file
            tempZipFile = File.createTempFile(zipFile.getName(), null, new File(zipFile.getParent()));
        } catch (Exception e) {
            e.printStackTrace();
            return;
        }

        try (FileOutputStream fos = new FileOutputStream(tempZipFile);
                BufferedOutputStream bos = new BufferedOutputStream(fos, buffer.length);
                ZipOutputStream zos = new ZipOutputStream(bos)) {

            if (zipFile.exists()) {
                try (FileInputStream fis = new FileInputStream(zipFilePath);
                        BufferedInputStream bis = new BufferedInputStream(fis);
                        ZipInputStream zin = new ZipInputStream(bis)) {

                    // Copy contents of input zip file to temp zip file
                    ZipEntry ze = null;
                    while ((ze = zin.getNextEntry()) != null) {
                        if (ze.getName().equalsIgnoreCase(entryName)) {
                            // The file we're trying to add already exists, so skip it
                            continue;
                        }

                        zos.putNextEntry(ze);
                        int len;
                        while ((len = zin.read(buffer)) > 0) {
                            zos.write(buffer, 0, len);
                        }
                        zos.closeEntry();
                    }
                }
            }

            // Add our new file
            zos.putNextEntry(new ZipEntry(entryName));
            try (FileInputStream fileFis = new FileInputStream(sourceFilePath);
                    BufferedInputStream fileBis = new BufferedInputStream(fileFis)) {
                int len;
                while ((len = fileBis.read(buffer)) > 0) {
                    zos.write(buffer, 0, len);
                }
            }
            zos.closeEntry();
            zos.finish();
            zos.close();
            bos.close();
            fos.close();

            // Copy temp file to original zip file
            if (zipFile.exists()) {
                zipFile.delete();
            }
            if (!tempZipFile.renameTo(zipFile)) {
                throw new Exception("Could not rename file " + tempZipFile.getName() + " to " + zipFile.getName());
            }
        } catch (Exception e) {
            e.printStackTrace();
        } finally {
            if (tempZipFile.exists()) {
                tempZipFile.delete();
            }
        }
    }

    private static void createDirIfNotExist(File path) {
        if (!path.exists()) {
            path.mkdirs();
        }
    }
}