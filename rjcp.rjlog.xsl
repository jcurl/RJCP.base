<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html" doctype-system="about:legacy-compat" encoding="UTF-8" indent="yes"/>

  <xsl:template match="rjbuildlog">
    <html>
      <head>
        <title>Build Log</title>
        <style>
          body {
            background-color:#d4d5e2;
            font-family: "Trebuchet MS", Helvetica, sans-serif;
            font-size: 0.75em;
          }
          h1 { 
            background-color:#013a58;
            color:white;
          }
          h2 { 
            background-color:#013a58;
            color:white;
          }
          h3 { 
            background-color:#235c7a;
            color:white;
          }
          table, th, td {
            border: 1px solid black;
            border-collapse: collapse;
            font-size: 1em;
          }
          th {
            text-align: left;
            background-color:#9fa4b2;
            color:black;
          }
          td {
            background-color:#bfc8d5;
            color:black;
          }
          pre {
            font-family: "DejaVu Sans Mono", "Lucida Console", Courier;
            font-size: 1em;
            overflow-x: auto;
            white-space: pre-wrap;
            white-space: -moz-pre-wrap;
            white-space: -pre-wrap;
            white-space: -o-pre-wrap;
            word-wrap: break-word;
          }
          .pass {
            background-color: #a0f0c0;
            color: #000000;
          }
          .fail {
            background-color: #f0a0c0;
            color: #ffffff;
          }
          .eventVerbose {
            background-color:#bfc8d5;
          }
          .eventInformation {
            background-color:#bfc8d5;
          }
          .eventWarning {
            background-color: #f0f0c0;
          }
          .eventError {
            background-color: #f0a0c0;
          }
          .eventCritical {
            background-color: #f0a0c0;
          }
        </style>
      </head>
      <body>
        <h1>General Information</h1>
        <table>
          <tr>
            <th>RjBuild Version:</th>
            <td><xsl:value-of select="@version"/></td>
          </tr>
          <tr>
            <th>Operating System:</th>
            <td><xsl:value-of select="machine/userdetails/operatingsystem"/></td>
          </tr>
          <tr>
            <th>Machine:</th>
            <td><xsl:value-of select="machine/userdetails/machinename"/> (<xsl:value-of select="machine/userdetails/processors"/> processors)</td>
          </tr>
          <tr>
            <th>User:</th>
            <td><xsl:value-of select="machine/userdetails/domain"/>\<xsl:value-of select="machine/userdetails/username"/></td>
          </tr>
        </table>
        <xsl:apply-templates select="buildsolution"/>
        <xsl:apply-templates select="eventlogs"/>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="buildsolution">
    <h1>Build Solution: <xsl:value-of select="@name"/></h1>
    <xsl:apply-templates select="build"/>
  </xsl:template>
  
  <xsl:template match="build">
    <h2>Build: <xsl:value-of select="@name"/></h2>
    <xsl:apply-templates select="msbuild"/>
    <xsl:apply-templates select="testresults"/>
  </xsl:template>
  
  <xsl:template match="msbuild">
    <xsl:apply-templates select="buildconfig"/>
    <xsl:apply-templates select="buildsteps"/>
    <xsl:apply-templates select="projects"/>
  </xsl:template>
  
  <xsl:template match="buildconfig">
    <h3>Configuration:</h3>
    <table class="configuration">
      <tr>
        <th>Solution:</th>
        <td><xsl:value-of select="solution"/></td>
      </tr>
      <tr>
        <th>Configuration:</th>
        <td><xsl:value-of select="configuration"/></td>
      </tr>
      <tr>
        <th>Output Directory:</th>
        <td><xsl:value-of select="outputdir"/></td>
      </tr>
      <tr>
        <th>Deployment Directory:</th>
        <td><xsl:value-of select="deploydir"/></td>
      </tr>
      <tr>
        <th>Tools Version:</th>
        <td><xsl:value-of select="toolsversion"/></td>
      </tr>
      <tr>
        <th>Target Framework:</th>
        <td><xsl:value-of select="targetframework"/></td>
      </tr>
    </table>
  </xsl:template>
  
  <xsl:template match="buildsteps">
    <h3>Build Steps:</h3>
    <table class="buildsteps" style="width:100%">
      <tr>
        <th colspan="2">Step</th>
        <th>Start (UTC)</th>
        <th>End (UTC)</th>
        <th>Duration (ms)</th>
        <th>Result</th>
      </tr>
      <xsl:apply-templates select="buildstep"/>
    </table>
  </xsl:template>
  
  <xsl:template match="buildstep">
    <tr>
      <th colspan="2" style="border-bottom:0"><xsl:value-of select="@name"/></th>
      <td><xsl:value-of select="@start"/></td>
      <td><xsl:value-of select="@end"/></td>
      <td><xsl:value-of select="@duration"/></td>
      <xsl:choose>
        <xsl:when test="@result != 0">
          <td class="fail"><xsl:value-of select="@result"/></td>
        </xsl:when>
        <xsl:otherwise>
          <td class="pass"><xsl:value-of select="@result"/></td>
        </xsl:otherwise>
      </xsl:choose>
    </tr>
    <tr>
      <th style="width:10px"></th>
      <td>Working Dir</td>
      <td colspan="4"><xsl:value-of select="workingdir"/></td>
    </tr>
    <tr>
      <th></th>
      <td>Command</td>
      <td colspan="4"><xsl:value-of select="command"/></td>
    </tr>
    <xsl:if test="@result != 0">
      <tr>
        <th></th>
        <td>Output</td>
        <td colspan="4"><pre><xsl:value-of select="output"/></pre></td>
      </tr>
    </xsl:if>
  </xsl:template>
  
  <xsl:template match="projects">
    <h3>Projects Built (and relevant test results):</h3>
    <table style="width:100%">
      <xsl:apply-templates select="project"/>
    </table>
  </xsl:template>
  
  <xsl:template match="project">
    <xsl:variable name="prjname" select="@name"/>
    <tr>
      <th colspan="2"></th>
      <th>Name</th>
      <th>Path</th>
      <th>Commit</th>
      <th>Dirty</th>
    </tr>
    <tr>
      <th colspan="2">Project:</th>
      <td colspan="4"><xsl:value-of select="@name"/> (version: <xsl:value-of select="target/@assemblyver"/>)</td>
    </tr>
    <tr>
      <th style="width:10px"></th>
      <th>Base:</th>
      <td><xsl:value-of select="@name"/></td>
      <td><xsl:value-of select="@dir"/></td>
      <td><xsl:value-of select="@commit"/> (<xsl:value-of select="@provider"/>)</td>
      <td><xsl:value-of select="@dirty"/></td>
    </tr>
    <tr>
      <th style="width:10px"></th>
      <th>Target:</th>
      <td><xsl:value-of select="target/@name"/></td>
      <td><xsl:value-of select="target/@dir"/></td>
      <td><xsl:value-of select="target/@commit"/></td>
      <td><xsl:value-of select="target/@dirty"/></td>
    </tr>
    <tr>
      <th style="width:10px"></th>
      <th>Deployment:</th>
      <xsl:choose>
        <xsl:when test="not(../../deployment/deploy[@project=$prjname])">
          <td class="fail" colspan="4">
            Copied: 0 files;
          </td>
        </xsl:when>
        <xsl:when test="../../deployment/deploy[@project=$prjname]/@result != 0">
          <td class="fail" colspan="4">
            Copied: <xsl:value-of select="count(../../deployment/deploy[@project=$prjname]/file)"/> files;
            (Result: <xsl:value-of select="../../deployment/deploy[@project=$prjname]/@result"/>)
            <ul style="color:black">
              <xsl:for-each select="../../deployment/deploy[@project=$prjname]/file[@copied='no']">
                <li><xsl:value-of select="@dest"/></li>
                <ul>
                  <li><xsl:value-of select="@description"/></li>
                </ul>
              </xsl:for-each>
            </ul>
          </td>
        </xsl:when>
        <xsl:otherwise>
          <td colspan="4">
            Copied: <xsl:value-of select="count(../../deployment/deploy[@project=$prjname]/file)"/> files;
            (Result: <xsl:value-of select="../../deployment/deploy[@project=$prjname]/@result"/>)
          </td>
        </xsl:otherwise>
      </xsl:choose>
    </tr>
    <xsl:apply-templates select="test"/>
    <xsl:call-template name="packages"/>
  </xsl:template>
  
  <xsl:template name="packages">
    <xsl:variable name="prjname" select="@name"/>
    <xsl:variable name="prjcommit" select="@commit"/>
    <xsl:variable name="id" select="../../../packages/package[@project=$prjname][@commit=$prjcommit]/@name"/>
    <xsl:choose>
      <xsl:when test="$id">
        <tr>
          <th style="width:10px"></th>
          <th>Package:</th>
          <td><xsl:value-of select="../../../packages/package[@project=$prjname]/@name"/> (version: <xsl:value-of select="../../../packages/package[@project=$prjname]/@version"/>)</td>
          <td><xsl:value-of select="../../../packages/package[@project=$prjname]/packageinfo/@spec"/></td>
          <td><xsl:value-of select="../../../packages/package[@project=$prjname]/@commit"/></td>
          <td><xsl:value-of select="../../../packages/package[@project=$prjname]/@tainted"/></td>
        </tr>
        <tr>
          <th></th>
          <td></td>
          <td colspan="4">
            <table class="projecttest" style="width:100%">
              <tr>
                <td style="width:120px">Duration (ms):</td>
                <td><xsl:value-of select="../../../packages/package[@project=$prjname]/@start"/> -
                    <xsl:value-of select="../../../packages/package[@project=$prjname]/@end"/>;
                    (<xsl:value-of select="../../../packages/package[@project=$prjname]/@duration"/> ms)</td>
                <xsl:choose>
                  <xsl:when test="./../../packages/package[@project=$prjname]/@result != 0">
                    <td class="fail" style="width:120px">Result: <xsl:value-of select="../../../packages/package[@project=$prjname]/@result"/></td>
                  </xsl:when>
                  <xsl:otherwise>
                    <td class="pass" style="width:120px">Result: <xsl:value-of select="../../../packages/package[@project=$prjname]/@result"/></td>
                  </xsl:otherwise>
                </xsl:choose>
              </tr>
              <tr>
                <td>Working Dir: </td>
                <td colspan="2"><xsl:value-of select="../../../packages/package[@project=$prjname]/workingdir"/></td>
              </tr>
              <tr>
                <td>Command: </td>
                <td colspan="2"><xsl:value-of select="../../../packages/package[@project=$prjname]/command"/></td>
              </tr>
              <xsl:if test="../../../packages/package[@project=$prjname]/@result != 0">
                <tr>
                  <td>Output: </td>
                  <td colspan="2"><pre><xsl:value-of select="../../../packages/package[@project=$prjname]/output"/></pre></td>
                </tr>
                <tr>
                  <td>Error: </td>
                  <td colspan="2"><pre><xsl:value-of select="../../../packages/package[@project=$prjname]/error"/></pre></td>
                </tr>
              </xsl:if>
              </table>
          </td>
        </tr>
      </xsl:when>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template match="test">
    <xsl:variable name="testname" select="@name"/>
    <xsl:variable name="testcommit" select="@commit"/>
    <xsl:variable name="testtype" select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/@testtype"/>
    <tr>
      <th style="width:10px"></th>
      <th>Test:</th>
      <td><xsl:value-of select="@name"/></td>
      <td><xsl:value-of select="@dir"/></td>
      <td><xsl:value-of select="@commit"/></td>
      <td><xsl:value-of select="@dirty"/></td>
    </tr>
    <xsl:if test="$testtype != ''">
      <tr>
        <th></th>
        <td></td>
        <td colspan="4">
          <table class="projecttest" style="width:100%">
            <tr>
              <td style="width:120px">Duration (ms):</td>
              <td><xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/@start"/> -
                  <xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/@end"/>;
                  (<xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/@duration"/> ms)</td>
              <td style="width:120px">Type: <xsl:value-of select="$testtype"/></td>
              <xsl:choose>
                <xsl:when test="../../../../testresults/test[@name=$testname][@commit=$testcommit]/@result != 0">
                  <td class="fail" style="width:120px">Result: <xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/@result"/></td>
                </xsl:when>
                <xsl:otherwise>
                  <td class="pass" style="width:120px">Result: <xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/@result"/></td>
                </xsl:otherwise>
              </xsl:choose>
            </tr>
            <tr>
              <td>Working Dir: </td>
              <td colspan="3"><xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/workingdir"/></td>
            </tr>
            <tr>
              <td>Command: </td>
              <td colspan="3"><xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/command"/></td>
            </tr>
            <xsl:if test="../../../../testresults/test[@name=$testname][@commit=$testcommit]/@result != 0">
              <tr>
                <td>Output: </td>
                <td colspan="3"><pre><xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/output"/></pre></td>
              </tr>
              <tr>
                <td>Error: </td>
                <td colspan="3"><pre><xsl:value-of select="../../../../testresults/test[@name=$testname][@commit=$testcommit]/error"/></pre></td>
              </tr>
            </xsl:if>
          </table>
        </td>
      </tr>
    </xsl:if>
  </xsl:template>

  <xsl:template match="testresults">
    <h3>Test Results:</h3>
    <table style="width:100%">
      <tr>
        <th>Start (UTC)</th>
        <th>End (UTC)</th>
        <th>Duration (ms)</th>
        <th>Name</th>
        <th>Project</th>
        <th>Type</th>
        <th>Result</th>
        <th>Commit</th>
        <th>Dirty</th>
      </tr>
      <xsl:for-each select="test">
        <xsl:sort select="@start"/>
        <xsl:call-template name="testresult"/>
      </xsl:for-each>
    </table>
  </xsl:template>
  
  <xsl:template name="testresult">
    <tr>
      <td><xsl:value-of select="@start"/></td>
      <td><xsl:value-of select="@end"/></td>
      <td><xsl:value-of select="@duration"/></td>
      <td><xsl:value-of select="@name"/></td>
      <td><xsl:value-of select="@project"/></td>
      <td><xsl:value-of select="@testtype"/></td>
      <xsl:choose>
        <xsl:when test="@result != 0">
          <td class="fail"><xsl:value-of select="@result"/></td>
        </xsl:when>
        <xsl:otherwise>
          <td><xsl:value-of select="@result"/></td>
        </xsl:otherwise>
      </xsl:choose>
      <td><xsl:value-of select="@commit"/></td>
      <td><xsl:value-of select="@dirty"/></td>
    </tr>
  </xsl:template>
  
  <xsl:template match="eventlogs">
    <h1>Event Log</h1>
    <table style="width:100%">
      <xsl:apply-templates select="eventlog"/>
    </table>
  </xsl:template>
  
  <xsl:template match="eventlog">
    <tr>
      <th>Time Stamp</th>
      <th>Severity</th>
      <th>Code</th>
      <th>File Name</th>
      <th>Description</th>
    </tr>
    <xsl:apply-templates select="event"/>
  </xsl:template>
  
  <xsl:template match="event">
    <tr>
      <td><xsl:value-of select="@datetime"/></td>
      <td class="event{@severity}"><xsl:value-of select="@severity"/></td>
      <td><xsl:value-of select="@code"/></td>
      <td><xsl:value-of select="@file"/></td>
      <td><xsl:value-of select="."/></td>
    </tr>
  </xsl:template>
</xsl:stylesheet>
