//package com.example.unityplugin;
package com.DefaultCompany.Sample3dAndroid;

import com.unity3d.player.UnityPlayer;
import android.content.Context;

public class ContextProvider {
    public static Context getContext() {
        return UnityPlayer.currentActivity;
    }
}