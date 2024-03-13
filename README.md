# 👻 ghost
Ghost is a quick few days project that has the pourpose of hiding applications from screen capture. *As a cronical online user (by ntts)*, I use discord and im most of the time sharing screen. This leads to sharing some information or applications such as password-managers been leaked. This is a simple solution to that problem & a cool project to work on.

## How it works
Well it's simple, but wasn't that simple at first. 

Windows introduced [**`SetWindowDisplayAffinity`**](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity) in Windows ~7, wich allows to hide applications from screen capture. This gave me the idea to create this application. Well, it appears i dind't think too much, and after having some base code, I found out that the `SetWindowDisplayAffinity` method only works on your own process. Wich makes sense because if not, any other application would be able to override your settings. Well i thougt it was over 😔. Looking at the window i thought ❕ *why dont I make a transparent overlay, so that the user can see through, put it over the target window, and then using `SetWindowDisplayAffinity` on my own window, I set the flag **WDA_MONITOR** to block whats behind* (*makes the window black*) and it worked 😲. 

It has few downsides comparing to other implemetations, such as injecting code to the target application to be able to execute the method from inside the target process.
The main downside would be that if the capture is directly focusing that window the overlay will not do its job.