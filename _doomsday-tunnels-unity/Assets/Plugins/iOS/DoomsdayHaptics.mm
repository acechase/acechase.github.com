// Haptics for Doomsday Tunnels 3D on iPhone: the system's impact and notification feedback generators, which play
// through the Taptic Engine. Called from Scripts/Input/Haptics.cs. Nothing here runs on any other platform.
#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *doomsdayImpact[3];
static UINotificationFeedbackGenerator *doomsdayNotify;

extern "C" {
    // style: 0 light, 1 medium, 2 heavy
    void DoomsdayHapticImpact(int style) {
        if (style < 0 || style > 2) style = 1;
        if (!doomsdayImpact[style]) {
            UIImpactFeedbackStyle s = style == 2 ? UIImpactFeedbackStyleHeavy : style == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleLight;
            doomsdayImpact[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
        }
        [doomsdayImpact[style] prepare];
        [doomsdayImpact[style] impactOccurred];
    }
    // type: 0 success, 1 warning, 2 error
    void DoomsdayHapticNotify(int type) {
        if (!doomsdayNotify) doomsdayNotify = [[UINotificationFeedbackGenerator alloc] init];
        [doomsdayNotify prepare];
        [doomsdayNotify notificationOccurred:(UINotificationFeedbackType)(type < 0 || type > 2 ? 1 : type)];
    }
}
