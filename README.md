# 👻 ghost
Ghost is a quick few days project that has the pourpose of hiding applications from screen capture. *As a cronical online user (by ntts)*, I use discord and im most of the time sharing screen. This leads to sharing some information or applications such as password-managers been leaked. This is a simple solution to that problem & a cool project to work on.

> ⚠️ For an unknown reason, the application is not working on **windows 10** or **below** systems. I'm working on a fix for this issue. If you have any information that could help me solve this issue, please let me know.

## 🤯 How it works
Well it's simple, but wasn't that simple at first. 

Windows introduced [**`SetWindowDisplayAffinity`**](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity) in Windows ~7, wich allows to hide applications from screen capture. This gave me the idea to create this application. Well, it appears i dind't think too much, and after having some base code, I found out that the `SetWindowDisplayAffinity` method only works on your own process. Wich makes sense because if not, any other application would be able to override your settings. Well i thougt it was over 😔. Looking at the window i thought ❕ *why dont I make a transparent overlay, so that the user can see through, put it over the target window, and then using `SetWindowDisplayAffinity` on my own window, I set the flag **WDA_MONITOR** to block whats behind* (*makes the window black*) and it worked 😲. 

It has few downsides comparing to other implemetations, such as injecting code to the target application to be able to execute the method from inside the target process.
The main downside would be that if the capture is directly focusing that window the overlay will not do its job. See velow pros & cons.

# 🎢 Pros & Cons
* (*high*) - It's *Really important*, you should take this into account before using the application
* (*moderate*) - It's important but *not a deal breaker*
* (*low*) - It's not that important but it's *good to know*

> ***Cons***
```diff
- (moderate) The overlay will not work if the capture is directly focusing the target window.

- (low) If you Alt + Tab or Win + Tab the application will be visible in the preview. (I'm working on a solution for this)

- (low) As this process is done externally, full-screen windows will not be covered. (just full-screen not borderless or maximized windows) 

- (low) The overlay might be a bit slow tracking the window position & size but most of the users wont notice any difference.

- (moderate) The window will appear with a black background in the capture, so it's not perfect for all situations.
```
> ***Pros***
```diff
+ (high) The overlay does not interact with the target application. So it's safe to use and will not risk any bans while playing games or using other applications that might have anti-cheat systems.

+ (high) This application will work on any application, even if it has elevated permissions or any flag that would prevent the anti capture method to work.

+ (moderate) You can set the application to run automatically on startup, in the background and have little interaction with the user so that it's always protecting your privacy.

+ (low) You can filter by window title match so you can hide certain applications or windows inside applications and also by process name.

+ (low) The application is very light and does not consume many resources.

+ (low) The application doesnt require any installation, just download and run.
```