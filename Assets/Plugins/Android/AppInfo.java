package aaa.QuestAppLauncher.App;

import com.unity3d.player.UnityPlayerActivity;
import android.app.Activity;
import android.content.pm.PackageManager;
import android.content.pm.PackageManager.NameNotFoundException;
import android.content.pm.PackageInfo;
import android.content.pm.FeatureInfo;
import android.content.pm.ApplicationInfo;
import android.os.Bundle;
import android.util.Log;
import android.graphics.Bitmap;
import android.graphics.drawable.BitmapDrawable;
import java.io.ByteArrayOutputStream;
import java.util.List;
import java.util.LinkedList;

public class AppInfo extends UnityPlayerActivity {

    private static final String TAG = "AppInfo";
    private List<ApplicationInfo> installedApps;

    @Override
    protected void onStart() {
        super.onStart();

        installedApps = new LinkedList<ApplicationInfo>();
        for(ApplicationInfo app : this.getPackageManager().getInstalledApplications(PackageManager.GET_META_DATA)) {
            if((app.flags & (ApplicationInfo.FLAG_UPDATED_SYSTEM_APP | ApplicationInfo.FLAG_SYSTEM)) > 0) {
                // Skip system app
                continue;
            }

            installedApps.add(app);
        }
    }

    public int getSize() {
        return this.installedApps.size();
    }

    public String getPackageName(int i) {
        return this.installedApps.get(i).packageName;
    }

    public String getAppName(int i) {
        return (String)this.getPackageManager().getApplicationLabel(installedApps.get(i));
    }

    public boolean isQuestApp(int i) {
        try {
            PackageInfo info = this.getPackageManager().getPackageInfo(getPackageName(i), 0);
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
        ApplicationInfo app = this.installedApps.get(i);
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
        BitmapDrawable icon = (BitmapDrawable)this.getPackageManager().getApplicationIcon(installedApps.get(i));
        Bitmap bmp = icon.getBitmap();
        ByteArrayOutputStream stream = new ByteArrayOutputStream();
        bmp.compress(Bitmap.CompressFormat.JPEG, 100, stream);
        byte[] byteArray = stream.toByteArray();
        return byteArray;
    }
}