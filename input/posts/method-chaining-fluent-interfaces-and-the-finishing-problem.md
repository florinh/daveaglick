﻿Title: Method Chaining, Fluent Interfaces, and the Finishing Problem
Lead: Or Why You Can't Have Your Cake And Eat It Too
Published: 5/30/2014
Tags:
  - fluent interfaces
  - method chaining
---
<p><a href="http://martinfowler.com/bliki/FluentInterface.html">Fluent interfaces</a> have become very popular in C# APIs recently. <a href="http://martinfowler.com/bliki/FluentInterface.html">Martin Fowler presumably coined the term in 2005</a> and at the time he wrote, “It's not a common style, but one we think should be better known”. Fluent interfaces are based on the older concepts of <a href="http://en.wikipedia.org/wiki/Method_chaining">method chaining</a> and <a href="http://en.wikipedia.org/wiki/Method_cascading">method cascading</a> (and the term has actually been misused quite a bit to refer to any type of method chaining), whereby the context of a call is passed through via method return values to the next method in the chain. This can result in a much more readable and concise API, particularly when many or a complex series of options or operations are available.</p>

<p>The focus of this blog post is on a particular challenge of fluent interfaces and method chaining known as the “finishing problem.” To illustrate it, consider a logging framework. It might allow some number of chained methods such as <code>Severity()</code>, <code>Source()</code>, <code>User()</code>, <code>CallSite()</code>, etc.:</p>

<pre class="prettyprint">Log.Message("Oh, noes!").Severity(Severity.Bad).User("jsmith");</pre>

<p>Looks nice, right? The problem here is that the logging framework doesn’t know when to write the log message to the log file. Do I do it in the <code>User()</code> method? What if I don’t use the <code>User()</code> method or I put it before the <code>Severity()</code> method, then when do I write to the file? This problem occurs any time you want the entire result of a method chain to take some external action other than manipulating the context of the chain.</p>

<p>There are a number of patterns for mitigating the finishing problem. Notice that I didn’t say <em>solving the finishing problem</em> – it turns out this can’t really be completely resolved, at least not generally.</p>

<h1>Terminating Method</h1>

<p>This first technique is probably one of the easier, but it’s also a bad <a href="http://en.wikipedia.org/wiki/Code_smell">code smell</a>. It requires the introduction of a method that serves to complete the chain and act on it’s final context. For example:</p>

<pre class="prettyprint">Log.Message("Oh, noes!").Severity(Severity.Bad).User("jsmith").Write();</pre>

<p>See how we added the <code>Write()</code> method there at the end? That <code>Write()</code> method takes the chain context, writes it to disk, and doesn’t return anything (effectively stopping the chain). So why is this so bad? For one, it would be very easy to forget the <code>Write()</code> method at the end of the chain. This technique requires the programmer to remember something that the compiler can’t check and that wouldn’t be picked up at runtime if they forgot. That’s a recipe for misery. It’s also superfluous. Why the heck do I need a <code>Write()</code> method if that’s the whole point of using the log in the first place?</p>

<h1>Method Argument</h1>

<p>This technique encloses the method chain inside a containing method that will be responsible for acting on the result of the chain. For example:</p>

<pre class="prettyprint">Log.Write(new Message("Oh, noes!").Severity(Severity.Bad).User("jsmith"));</pre>

<p>In this case, the <code>Log.Write()</code> method accepts an argument of the type that the chain passes along. Because the whole chain is an input to the <code>Write()</code> method, it can act on the result and write to the file. The downside to this technique is that you have to instantiate an object to pass to the <code>Write()</code> method. It’s also not particularly elegant.</p>

<h1>Delegate Argument</h1>

<p>This technique is very similar to the last one except that instead of passing in a newly instantiated object, one is instantiated by the <code>Write()</code> method and passed to a delegate that manipulates it before being acted on by <code>Write()</code>. The use of lambdas makes this look fairly tidy:</p>

<pre class="prettyprint">Log.Write(x => x.Message("Oh, noes!").Severity(Severity.Bad).User("jsmith"));</pre>

<p>This has an advantage over the previous technique in that the argument to the delegate is already typed and so Intellisense can be used to provide a slightly better experience (instead of having to known what types of objects should be instantiated and passed to the method). Of course, the downside is that the syntax is getting even further afield of the nice concise ideal.</p>

<h1>Casting</h1>

<p>I have not seen mention of this technique anywhere else, probably because it only works well in specific scenarios. The idea is that the external action is taken upon casting the chain context object to some other type via a casting operator. For example:</p>

<pre class="prettyprint">LogWriter writer = (LogWriter)Log.Message("Oh, noes!").Severity(Severity.Bad).User("jsmith");</code>></pre>

<p>Inside the chain context class (let’s call it LogOptions) you might have something like:</p>

<pre class="prettyprint">public class LogOptions
{
  public static string LogPath { get; set; }

  public static implicit operator LogWriter(LogOptions ops)
  {
    File.AppendAllText(LogPath, ops.ToString());
    return new LogWriter(ops);
  }
}</pre>

<p>This technique really only makes sense if you’re actually going to do something with that <code>LogWriter</code> instance. If not, it’s going to create a bunch of squiggles in Visual Studio telling you it’s an unused variable and drive you nuts. However, this can be valuable if you’re working with another API that you know expects certain types of objects.</p>

<h1>Rewinding</h1>

<p>In very specific situations you may be able to essentially “undo” the result of the previous method in the chain on each subsequent one. This lets you keep the original ideal syntax:</p>

<pre class="prettyprint">Log.Message("Oh, noes!").Severity(Severity.Bad).User("jsmith");</pre>

<p>Then, inside each method <code>Message()</code>, <code>Severity()</code>, and <code>User()</code>, check to see if it’s the first call in the chain (this can be determined by setting a flag in the chain context object on each chained method – if the flag isn’t set, this is the first method in the chain). If it’s not the first method in the chain, undo whatever the previous method did before doing it again with the new context state. For example, in the log scenario remove the last line in the file before appending a new replacement one (obviously you wouldn’t actually want to do this for a log file).</p>

<h1>Buffering</h1>

<p>You may be able to buffer the result of the chained methods until some other action that you know is going to happen takes place. For example, in the log example, the methods <code>Message()</code>, <code>Severity()</code>, and <code>User()</code> could add their respective state information to the <code>Log</code> instance and then output the log message <em>on the next</em> call to <code>Log.Message()</code>. You obviously wouldn't want to do that for this specific example because your log messages would get delayed and possibly never sent, but hopefully you get the idea. As it turns out, I actually used a form of this approach to handle the fluent interfaces in <a href="http://fluentbootstrap.com">FluentBootstrap</a>.</p>

<h1>Don’t Use a Fluent Interface</h1>

<p>It seems like everyone is trying to introduce method chains and fluent interfaces into their APIs recently. In my opinion, this often just causes more trouble than it solves. Maintaining the context objects for a complex fluent interface can be challenging and with the availability of named and optional arguments in C# methods now, I rarely see much syntactic benefit. To me, the following syntax with named arguments is just as terse and understandable as the fluent interface version (if not more so):</p>

<pre class="prettyprint">Log.Message("Oh, noes!", severity: Severty.Bad, user: "jsmith");</pre>

<p>In general, I’ve found fluent interfaces work very well for populating or initializing some set of options within a settings object that you’re going to pass around. As soon as you start trying to introduce external logic into the picture they become much more difficult to get right. Sometimes the best solution is to rethink the problem.</p>